using org.mbarbon.p.values;
using System.Reflection;
using System.Collections.Generic;
using Type = System.Type;

namespace org.mbarbon.p.runtime
{
    public class NetGlue
    {
        private static bool IsAnyType(Type type)
        {
            return    type == typeof(bool)
                   || type == typeof(string)
                   || type == typeof(char);
        }

        private static bool IsIntegerType(Type type)
        {
            return type == typeof(int) || type.IsEnum;
        }

        private static int CompareSignatures(ParameterInfo[] a, ParameterInfo[] b)
        {
            if (a.Length != b.Length)
                return a.Length - b.Length;

            for (int i = 0; i < a.Length; ++i)
            {
                Type at = a[i].ParameterType, bt = b[i].ParameterType;
                bool aa = IsAnyType(at), ba = IsAnyType(bt);
                bool ai = IsIntegerType(at), bi = IsIntegerType(bt);

                // TODO handle subtype sorting

                // string/char/bool sort at the end
                if ( aa && !ba) return  1;
                if (!aa &&  ba) return -1;

                // then integer types
                if ( ai && !bi) return  1;
                if (!ai &&  bi) return -1;

                // other types aren't tie-breakers
            }

            return 0;
        }

        private static MethodBase[] SortMethods(MethodBase[] methods)
        {
            System.Array.Sort(methods, delegate(MethodBase a, MethodBase b)
                              {
                                  return CompareSignatures(a.GetParameters(),
                                                           b.GetParameters());
                              });

            return methods;
        }

        private static bool Matches(Runtime runtime, Type type, IP5Any value)
        {
            var scalar = value as P5Scalar;

            if (   type == typeof(bool)
                || type == typeof(string)
                || type == typeof(char))
                return true;

            if (type == typeof(int) || type.IsEnum)
            {
                if (   scalar != null
                    && !scalar.IsInteger(runtime)
                    && !scalar.IsFloat(runtime))
                    return false;

                return true;
            }

            // treat undef as null
            if (scalar != null && !scalar.IsDefined(runtime))
                return true;

            if (type.IsArray)
            {
                var refbody = scalar.Body as P5Reference;
                var wrapper = scalar.Body as P5NetWrapper;

                if (refbody != null)
                {
                    var array = refbody.Referred as IP5Array;

                    if (array != null)
                        return true;
                }

                if (wrapper != null)
                    return wrapper.Object.GetType().IsArray;
            }

            if (typeof(IP5Any).IsAssignableFrom(type))
            {
                if (type == value.GetType())
                    return true;
            }

            var net_wrapper = scalar.NetWrapper(runtime);
            if (net_wrapper == null)
                return false;
            if (!type.IsAssignableFrom(net_wrapper.Object.GetType()))
                return false;

            return true;
        }

        private static bool Matches(Runtime runtime, MethodBase meth, P5Scalar[] args)
        {
            var parms = meth.GetParameters();

            int index = 0;
            // special case: if the first argument is a Runtime, pass
            // it implicity
            if (parms.Length > 0 && parms[0].ParameterType == typeof(Runtime))
                index = -1;
            if (parms.Length + index != args.Length)
                return false;

            foreach (var parm in parms)
            {
                if (index < 0)
                    continue;
                if (!Matches(runtime, parm.ParameterType, args[index]))
                    return false;
                ++index;
            }

            return true;
        }

        private static object Convert(Runtime runtime, IP5Any arg, Type type)
        {
            if (type == typeof(int))
                return arg.AsInteger(runtime);
            if (type.IsEnum)
                return System.Enum.ToObject(type, arg.AsInteger(runtime));
            if (type == typeof(char))
                return arg.AsString(runtime)[0];
            if (type == typeof(bool))
                return arg.AsBoolean(runtime);
            if (type == typeof(string))
                return arg.AsString(runtime);

            var scalar = arg as P5Scalar;

            // treat undef as null
            if (scalar != null && !scalar.IsDefined(runtime))
                return null;

            if (typeof(IP5Any).IsAssignableFrom(type))
                return arg;

            // array
            if (type.IsArray)
                return UnwrapArray(runtime, scalar, type.GetElementType());

            // fallback
            var net_wrapper = scalar.NetWrapper(runtime);

            return net_wrapper.Object;
        }

        private static object[] ConvertArgs(Runtime runtime, MethodBase meth, P5Scalar[] args)
        {
            var parms = meth.GetParameters();
            var res = new object[parms.Length];

            int offset = 0;
            // see the comment in Matches()
            if (parms.Length > 0 && parms[0].ParameterType == typeof(Runtime))
            {
                offset = 1;
                res[0] = runtime;
            }
            for (int i = 0; i < args.Length; ++i)
                res[i + offset] = Convert(runtime, args[i], parms[i + offset].ParameterType);

            return res;
        }

        public static IP5Any GetClass(Runtime runtime, string name)
        {
            var cls = System.Type.GetType(name);
            var wrapper = new P5NetWrapper(cls);

            return new P5Scalar(wrapper);
        }

        private static IP5Any WrapNewNoBless(Runtime runtime, Opcode.ContextValues context,
                                             P5ScratchPad pad, P5Array args)
        {
            var count = args.GetCount(runtime);
            var arg = new P5Scalar[count - 1];

            for (int i = 1; i < count; ++i)
                arg[i - 1] = args.GetItem(runtime, i) as P5Scalar;

            var cls = pad[0] as P5Scalar;
            var res = CallConstructor(runtime, cls, arg);

            return res;
        }

        private static IP5Any WrapNew(Runtime runtime, Opcode.ContextValues context,
                                      P5ScratchPad pad, P5Array args)
        {
            var count = args.GetCount(runtime);
            var arg = new P5Scalar[count - 1];

            for (int i = 1; i < count; ++i)
                arg[i - 1] = args.GetItem(runtime, i) as P5Scalar;

            var cls = pad[0] as P5Scalar;
            var pack = pad[1] as P5SymbolTable;
            var val = CallConstructor(runtime, cls, arg);
            var res = new P5Scalar(runtime, val);

            val.Bless(runtime, pack);

            return res;
        }

        public static IP5Any Extend(Runtime runtime, string pack, string name,
                                    string method, bool bless)
        {
            var cls = System.Type.GetType(name);
            var wrapper = new P5Scalar(new P5NetWrapper(cls));
            var stash = runtime.SymbolTable.GetPackage(runtime, pack);
            var pad = new P5ScratchPad();

            pad.Add(wrapper);
            if (bless)
                pad.Add(stash);

            var glob = stash.GetStashGlob(runtime, method, true);
            // var code = new P5NativeCode(pack + "::" + method,
            //                             bless ?
            //                                 new P5Code.Sub(WrapNew) :
            //                                 new P5Code.Sub(WrapNewNoBless));

//            code.ScratchPad = pad;
//            glob.Code = code;

            return new P5Scalar(runtime);
        }

        public static IP5Any CallConstructor(Runtime runtime, P5Scalar wrapper,
                                             P5Scalar[] args)
        {
            var net_wrapper = wrapper.NetWrapper(runtime);
            var type = net_wrapper.Object as System.Type;

            return CallConstructor(runtime, type, args);
        }

        public static IP5Any CallConstructor(Runtime runtime, System.Type type,
                                             P5Scalar[] args)
        {
            foreach (var ctor in SortMethods(type.GetConstructors()))
            {
                if (!Matches(runtime, ctor, args))
                    continue;
                var net_args = ConvertArgs(runtime, ctor, args);

                var res = ((ConstructorInfo)ctor).Invoke(net_args);

                return WrapValue(res);
            }

            throw new System.Exception("Constructor not found");
        }

        public static IP5Any CallMethod(Runtime runtime, P5Scalar wrapper,
                                        string method, P5Scalar[] args)
        {
            var net_wrapper = wrapper.NetWrapper(runtime);
            var obj = net_wrapper.Object;

            return CallMethod(runtime, obj, method, args);
        }

        public static IP5Any CallMethod(Runtime runtime, object obj,
                                        string method, P5Scalar[] args)
        {
            var type = obj.GetType();

            foreach (var meth in SortMethods(type.GetMethods()))
            {
                if (meth.Name != method)
                    continue;
                if (!Matches(runtime, meth, args))
                    continue;
                var net_args = ConvertArgs(runtime, meth, args);

                var res = meth.Invoke(obj, net_args);

                return WrapValue(res);
            }

            throw new P5Exception(runtime, string.Format("Can't locate object method \"{0:S}\" via type \"{1:S}\"", method, type.FullName));
        }

        public static IP5Any CallStaticMethod(Runtime runtime, System.Type type,
                                              string method, P5Scalar[] args)
        {
            foreach (var meth in SortMethods(type.GetMethods(BindingFlags.FlattenHierarchy|BindingFlags.Public|BindingFlags.Static)))
            {
                if (meth.Name != method)
                    continue;
                if (!Matches(runtime, meth, args))
                    continue;
                var net_args = ConvertArgs(runtime, meth, args);

                var res = meth.Invoke(null, net_args);

                return WrapValue(res);
            }

            throw new P5Exception(runtime, string.Format("Can't locate object method \"{0:S}\" via type \"{1:S}\"", method, type.FullName));
        }

        public static IP5Any GetProperty(Runtime runtime, P5Scalar wrapper,
                                         string name)
        {
            var net_wrapper = wrapper.NetWrapper(runtime);
            var obj = net_wrapper.Object;
            var prop = obj.GetType().GetProperty(name);

            var res = prop.GetValue(obj, null);

            return WrapValue(res);
        }

        public static void SetProperty(Runtime runtime, P5Scalar wrapper,
                                       string name, IP5Any value)
        {
            var net_wrapper = wrapper.NetWrapper(runtime);
            var obj = net_wrapper.Object;
            var prop = obj.GetType().GetProperty(name);

            if (!Matches(runtime, prop.PropertyType, value))
                throw new System.Exception("Invalid type");

            prop.SetValue(obj, Convert(runtime, value, prop.PropertyType), null);
        }

        public static object UnwrapValue(object value, System.Type type)
        {
            return UnwrapValue(null, value, type);
        }

        public static T UnwrapValue<T>(Runtime runtime, object value)
        {
            return (T)UnwrapValue(runtime, value, typeof(T));
        }

        public static object UnwrapValue(Runtime runtime, object obj,
                                         System.Type type)
        {
            var value = obj as IP5Any;
            if (value == null)
                return obj;

            var scalar = value as P5Scalar;
            if (scalar == null)
                throw new System.NotImplementedException("Can only unwrap scalars");

            // TODO more integral types
            if (runtime != null)
            {
                if (type.IsEnum)
                    return System.Enum.ToObject(type, value.AsInteger(runtime));
                if (type == typeof(int))
                    return value.AsInteger(runtime);
                if (type == typeof(char))
                    return (char)value.AsInteger(runtime);
            }

            // automatically dereference values created by Extend
            var refbody = scalar.Body as P5Reference;
            if (refbody != null)
            {
                scalar = refbody.Referred as P5Scalar;
                if (scalar == null)
                {
                    var array = refbody.Referred as IP5Array;

                    // TODO check element type
                    if (array == null)
                        throw new System.NotImplementedException(string.Format("Can't unwrap reference to {0:S} as {1:S}", refbody.Referred.GetType(), type));

                    var element_type = GetListType(type);
                    if (element_type != null)
                        return UnwrapList(runtime, refbody.Referred as IP5Any, element_type);

                    throw new System.NotImplementedException(string.Format("Can't unwrap reference to {0:S} as {1:S}", refbody.Referred.GetType(), type));
                }
            }

            if (!scalar.IsDefined(runtime))
                return null;

            var wrapper = scalar.Body as P5NetWrapper;
            if (wrapper == null)
            {
                if (type.IsAssignableFrom(value.GetType()))
                    return value;

                throw new System.NotImplementedException(string.Format("Can't coerce {0:S} to {1:S}", value.GetType(), type));
            }

            if (type.IsAssignableFrom(wrapper.Object.GetType()))
                return wrapper.Object;

            throw new System.NotImplementedException(string.Format("Can't coerce {0:S} to {1:S}", wrapper.Object.GetType(), type));
        }

        public static P5Scalar WrapValue(object value)
        {
            if (value == null)
                return P5Scalar.Undef();

            P5Scalar scalar = value as P5Scalar;
            if (scalar != null)
                return scalar;

            return new P5Scalar(new P5NetWrapper(value));
        }

        public static System.Collections.IList UnwrapList(Runtime runtime, IP5Any arr, System.Type type)
        {
            IP5Array array = arr as IP5Array;

            // TODO dereference
            var list_type = typeof(System.Collections.Generic.List<>).MakeGenericType(type);
            System.Collections.IList list = list_type.GetConstructor(new System.Type[0]).Invoke(new object[0]) as System.Collections.IList;

            foreach (var obj in array)
                list.Add(UnwrapValue(runtime, obj, type));

            return list;
        }

        public static T[] UnwrapArray<T>(Runtime runtime, IP5Any arr)
        {
            return (T[])UnwrapArray(runtime, arr, typeof(T));
        }

        public static System.Array UnwrapArray(Runtime runtime, IP5Any arr,
                                               Type type)
        {
            IP5Array array = arr as IP5Array;

            if (array == null)
            {
                P5Scalar scalar = arr as P5Scalar;

                array = scalar.DereferenceArray(runtime);
            }

            int count = array.GetCount(runtime);
            var iter = array.GetEnumerator();

            var values = System.Array.CreateInstance(type, count);

            for (int i = 0; i < count && iter.MoveNext(); ++i)
                values.SetValue(UnwrapValue(runtime, iter.Current, type), i);

            return values;
        }

        public static System.Type GetListType(object obj)
        {
            if ((obj as System.Collections.IList) == null)
                return null;

            return GetListType(obj.GetType());
        }

        public static System.Type GetListType(System.Type list_type)
        {
            // TODO maybe cache this check
            var type = typeof(object);

            foreach (var iface in list_type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IList<>))
                {
                    type = iface.GetGenericArguments()[0];
                    break;
                }
            }

            return type;
        }
    }
}
