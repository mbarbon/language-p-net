using Runtime = org.mbarbon.p.runtime.Runtime;

namespace org.mbarbon.p.values
{
    public class P5Undef : IP5ScalarBody
    {
        public P5Undef(Runtime runtime)
        {
        }

        public virtual IP5ScalarBody CloneBody(Runtime runtime)
        {
            return new P5Undef(runtime);
        }

        public virtual string AsString(Runtime runtime) { return ""; }
        public virtual int AsInteger(Runtime runtime) { return 0; }
        public virtual double AsFloat(Runtime runtime) { return 0.0; }
        public virtual bool AsBoolean(Runtime runtime) { return false; }
        public virtual int Length(Runtime runtime) { return 0; }

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
    }
}
