using Runtime = org.mbarbon.p.runtime.Runtime;
using Builtins = org.mbarbon.p.runtime.Builtins;
using System.Collections.Generic;

namespace org.mbarbon.p.values
{
    public class P5NetWrapper : IP5ScalarBody
    {
        public P5NetWrapper(Runtime runtime, object _obj)
        {
            obj = _obj;
        }

        public IP5ScalarBody CloneBody(Runtime runtime)
        {
            return this;
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

            throw new System.NotImplementedException();
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
            // TODO string

            throw new System.NotImplementedException();
        }

        public bool AsBoolean(Runtime runtime)
        {
            return true;
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

        public P5Scalar ReferenceType(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public P5Scalar DereferenceScalar(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public P5Array DereferenceArray(Runtime runtime)
        {
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

        public object Object
        {
            get { return obj; }
        }

        private object obj;
    }
}
