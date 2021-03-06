using Runtime = org.mbarbon.p.runtime.Runtime;
using Opcode = org.mbarbon.p.runtime.Opcode;
using Builtins = org.mbarbon.p.runtime.Builtins;
using NetGlue = org.mbarbon.p.runtime.NetGlue;
using IdDispenser = Microsoft.Scripting.Runtime.IdDispenser;
using System.Collections.Generic;

namespace org.mbarbon.p.values
{
    public class P5NetWrapper : IP5ScalarBody
    {
        public P5NetWrapper(object _obj)
        {
            obj = _obj;
        }

        public IP5ScalarBody CloneBody(Runtime runtime)
        {
            return this;
        }

        public virtual IP5ScalarBody Assign(Runtime runtime, IP5ScalarBody other)
        {
            return other.CloneBody(runtime);
        }

        public string AsString(Runtime runtime)
        {
            return obj.ToString();
        }

        public int AsInteger(Runtime runtime)
        {
            var type = obj.GetType();

            if (type == typeof(int))
                return (int)obj;
            if (type == typeof(double))
                return (int)(double)obj;
            if (type == typeof(char))
                return (int)(char)obj;
            if (type == typeof(bool))
                return (bool)obj ? 1 : 0;
            if (type == typeof(string))
                return Builtins.ParseInteger((string)obj);
            if (type.IsEnum)
                return System.Convert.ToInt32(obj);

            throw new System.NotImplementedException(string.Format("Integer coercion not implemented for {0:S}", type));
        }

        public double AsFloat(Runtime runtime)
        {
            var type = obj.GetType();

            if (type == typeof(double))
                return (double)obj;
            if (type == typeof(int))
                return (double)(int)obj;
            if (type == typeof(char))
                return (double)(char)obj;
            if (type == typeof(bool))
                return (bool)obj ? 1.0 : 0.0;
            if (type.IsEnum)
                return System.Convert.ToDouble(obj);

            // TODO string

            throw new System.NotImplementedException(string.Format("Float coercion not implemented for {0:S}", type));
        }

        public bool AsBoolean(Runtime runtime)
        {
            var type = obj.GetType();

            if (type == typeof(double))
                return (double)obj != 0.0;
            if (type == typeof(int))
                return (int)obj != 0;
            if (type == typeof(string))
                return (string)obj != "";
            if (type == typeof(bool))
                return (bool)obj;

            return obj != null;
        }

        public int Length(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public bool IsInteger(Runtime runtime)
        {
            var type = obj.GetType();

            return    type == typeof(int) || type == typeof(char)
                   || type == typeof(bool);
        }

        public bool IsString(Runtime runtime)
        {
            var type = obj.GetType();

            return type == typeof(string);
        }

        public bool IsFloat(Runtime runtime)
        {
            var type = obj.GetType();

            return type == typeof(double);
        }

        public int GetPos(Runtime runtime)
        {
            return -1;
        }

        public int GetPos(Runtime runtime, out bool pos_set)
        {
            pos_set = false;

            return -1;
        }

        public void SetPos(Runtime runtime, int pos, bool pos_set)
        {
            throw new System.NotImplementedException();
        }

        public virtual string KeyString(Runtime runtime)
        {
            return IdDispenser.GetId(Object).ToString();
        }

        public string ReferenceTypeString(Runtime runtime)
        {
            return obj.GetType().FullName;
        }

        public P5Scalar DereferenceScalar(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public IP5Array DereferenceArray(Runtime runtime)
        {
            var array = obj as System.Array;
            if (array != null)
                return new P5NetArray(array, obj.GetType().GetElementType());

            var type = NetGlue.GetListType(obj);
            if (type != null)
                return new P5NetArray(obj as System.Collections.IList, type);

            throw new System.NotImplementedException();
        }

        public P5Hash DereferenceHash(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public P5Typeglob DereferenceGlob(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public P5Code DereferenceSubroutine(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public P5Handle DereferenceHandle(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public IP5Any CallMethod(Runtime runtime, Opcode.ContextValues context,
                                 string method, P5Array args)
        {
            int count = args.GetCount(runtime);
            var arg = new P5Scalar[count - 1];

            for (int i = 1; i < count; ++i)
                arg[i - 1] = args.GetItem(runtime, i) as P5Scalar;

            var type = obj as System.Type;
            if (type != null)
                return NetGlue.CallStaticMethod(runtime, type, method, arg);
            else
                return NetGlue.CallMethod(runtime, obj, method, arg);
        }

        public object Object
        {
            get { return obj; }
        }

        private object obj;
    }
}
