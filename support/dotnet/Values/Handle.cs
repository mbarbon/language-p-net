using org.mbarbon.p.runtime;
using System.Collections.Generic;

namespace org.mbarbon.p.values
{
    public class P5Handle : IP5Any
    {
        public P5Handle(Runtime runtime)
        {
        }

        public int Write(Runtime runtime, IP5Any scalar, int offset, int length)
        {
            // FIXME cheating
            System.Console.Write(scalar.AsString(runtime));

            return 1;
        }

        // FIXME proper implementation
        public virtual P5Scalar AsScalar(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual string AsString(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual int AsInteger(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual double AsFloat(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual bool AsBoolean(Runtime runtime) { return true; }
        public virtual bool IsDefined(Runtime runtime) { return true; }

        public virtual IP5Any Clone(Runtime runtime, int depth)
        {
            return new P5Handle(runtime);
        }

        public virtual IP5Any Localize(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public virtual IP5Any Assign(Runtime runtime, IP5Any other)
        {
            throw new System.NotImplementedException();
        }

        public virtual IP5Any ConcatAssign(Runtime runtime, IP5Any other)
        {
            throw new System.InvalidOperationException();
        }

        public virtual IP5Any AssignIterator(Runtime runtime, IEnumerator<IP5Any> iter)
        {
            return Assign(runtime, iter.MoveNext() ? iter.Current : new P5Scalar(runtime));
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

        public virtual P5Scalar VivifyScalar(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Array VivifyArray(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Hash VivifyHash(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Code FindMethod(Runtime runtime, string method)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual void Bless(Runtime runtime, P5SymbolTable stash)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual bool IsBlessed(Runtime runtime)
        {
            return false;
        }

        public virtual P5SymbolTable Blessed(Runtime runtime)
        {
            return null;
        }
    }
}
