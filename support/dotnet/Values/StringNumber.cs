using Runtime = org.mbarbon.p.runtime.Runtime;

namespace org.mbarbon.p.values
{
    public class P5StringNumber : IP5ScalarBody
    {
        internal const int HasString  = 1;
        internal const int HasFloat   = 2;
        internal const int HasInteger = 4;

        public P5StringNumber(Runtime runtime, int val)
        {
            flags = HasInteger;
            integerValue = val;
        }

        public P5StringNumber(Runtime runtime, string val)
        {
            flags = HasString;
            stringValue = val;
        }

        public P5StringNumber(Runtime runtime, double val)
        {
            flags = HasFloat;
            floatValue = val;
        }

        private P5StringNumber(Runtime runtime, int f, int ival, string sval, double fval)
        {
            flags = f;
            integerValue = ival;
            stringValue = sval;
            floatValue = fval;
        }

        public virtual string AsString(Runtime runtime)
        {
            if ((flags & HasString) != 0) return stringValue;
            if ((flags & HasInteger) != 0) return System.String.Format("{0:D}", integerValue);
            if ((flags & HasFloat) != 0) return System.String.Format("{0:0.#########}", floatValue);

            throw new System.Exception();
        }

        public virtual int AsInteger(Runtime runtime)
        {
            if ((flags & HasString) != 0) return System.Int32.Parse(stringValue);
            if ((flags & HasInteger) != 0) return integerValue;
            if ((flags & HasFloat) != 0) return (int)floatValue;

            throw new System.Exception();
        }

        public virtual double AsFloat(Runtime runtime)
        {
            if ((flags & HasString) != 0) return System.Double.Parse(stringValue);
            if ((flags & HasInteger) != 0) return integerValue;
            if ((flags & HasFloat) != 0) return floatValue;

            throw new System.Exception();
        }

        public virtual bool AsBoolean(Runtime runtime)
        {
            return    ((flags & HasInteger) != 0 && integerValue != 0)
                   || ((flags & HasString) != 0 && stringValue.Length != 0)
                   || ((flags & HasFloat) != 0 && floatValue != 0);
        }

        public virtual IP5ScalarBody CloneBody(Runtime runtime)
        {
            return new P5StringNumber(runtime, flags, integerValue, stringValue, floatValue);
        }

        internal void Increment(Runtime runtime)
        {
            if ((flags & HasFloat) != 0)
                floatValue = floatValue + 1.0;
            if ((flags & HasInteger) != 0)
                integerValue = integerValue + 1;
            // TODO string increment
        }

        internal void Decrement(Runtime runtime)
        {
            if ((flags & HasFloat) != 0)
                floatValue = floatValue - 1.0;
            if ((flags & HasInteger) != 0)
                integerValue = integerValue - 1;
            // TODO string decrement
        }

        public virtual P5Scalar ReferenceType(Runtime runtime)
        {
            return new P5Scalar(runtime);
        }

        public virtual P5Scalar DereferenceScalar(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Array DereferenceArray(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Hash DereferenceHash(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Typeglob DereferenceGlob(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Code DereferenceSubroutine(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        internal int flags;
        internal string stringValue;
        internal int integerValue;
        internal double floatValue;
    }
}
