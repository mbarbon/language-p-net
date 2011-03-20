using org.mbarbon.p.values;
using System.Reflection;
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
            return type == typeof(int);
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

            if (type == typeof(int))
            {
                if (scalar != null && !scalar.IsInteger(runtime))
                    return false;

                return true;
            }

            // treat undef as null
            if (scalar != null && !scalar.IsDefined(runtime))
                return true;

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
            var code = new P5NativeCode(pack + "::" + method,
                                        bless ?
                                            new P5Code.Sub(WrapNew) :
                                            new P5Code.Sub(WrapNewNoBless));

            code.ScratchPad = pad;
            glob.Code = code;

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

                var res = ctor.Invoke(null, net_args);

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

        public static object UnwrapValue(IP5Any value, System.Type type)
        {
            if (value == null)
                return null;

            var scalar = value as P5Scalar;
            if (scalar == null)
                return null;

            // automatically dereference values created by Extend
            var refbody = scalar.Body as P5Reference;
            if (refbody != null)
            {
                scalar = refbody.Referred as P5Scalar;
                if (scalar == null)
                    return null;
            }

            var wrapper = scalar.Body as P5NetWrapper;
            if (wrapper == null)
            {
                if (type.IsAssignableFrom(value.GetType()))
                    return value;

                return null;
            }

            if (type.IsAssignableFrom(wrapper.Object.GetType()))
                return wrapper.Object;

            return null;
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
