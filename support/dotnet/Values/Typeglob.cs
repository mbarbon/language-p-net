using Runtime = org.mbarbon.p.runtime.Runtime;

namespace org.mbarbon.p.values
{
    public class P5Typeglob : P5Scalar
    {
        public P5Typeglob(Runtime runtime, string name)
        {
            body = globBody = new P5TypeglobBody(runtime, name);
        }

        public P5Scalar Scalar
        {
            get { return globBody.Scalar; }
            set { globBody.Scalar = value; }
        }

        public P5Scalar GetScalar(Runtime runtime)
        {
            return globBody.Scalar ??
                (globBody.Scalar = new P5Scalar(runtime));
        }

        public P5Array Array
        {
            get { return globBody.Array; }
            set { globBody.Array = value; }
        }

        public P5Array GetArray(Runtime runtime)
        {
            return globBody.Array ??
                (globBody.Array = new P5Array(runtime));
        }

        public P5Hash Hash
        {
            get { return globBody.Hash; }
            set { globBody.Hash = value; }
        }

        public P5Hash GetHash(Runtime runtime)
        {
            return globBody.Hash ??
                (globBody.Hash = new P5Hash(runtime));
        }

        public P5Handle Handle
        {
            get { return globBody.Handle; }
            set { globBody.Handle = value; }
        }

        public P5Handle GetHandle(Runtime runtime)
        {
            return globBody.Handle ??
                (globBody.Handle = new P5Handle(runtime, null, null));
        }

        public P5Code Code
        {
            get { return globBody.Code; }
            set { globBody.Code = value; }
        }

        public P5Code GetCode(Runtime runtime)
        {
            return globBody.Code ??
                (globBody.Code = new P5Code(globBody.Name, null));
        }

        public override P5Scalar Assign(Runtime runtime, IP5Any other)
        {
            var ob = other.AsScalar(runtime).Body;
            var obr = ob as P5Reference;
            var obb = ob as P5TypeglobBody;

            if (obb != null)
                body = globBody = obb;
            else if (obr != null)
            {
                var referred = obr.Referred;
                var code = referred as P5Code;
                var scalar = referred as P5Scalar;
                var array = referred as P5Array;
                var hash = referred as P5Hash;

                if (code != null)
                    globBody.Code = code;
                else if (scalar != null)
                    globBody.Scalar = scalar;
                else if (array != null)
                    globBody.Array = array;
                else if (hash != null)
                    globBody.Hash = hash;
            }
            else
            {
                throw new System.NotImplementedException("Assign either glob or reference");
            }

            return this;
        }

        private P5TypeglobBody globBody;
    }

    public class P5TypeglobBody : IP5ScalarBody
    {
        public P5TypeglobBody(Runtime runtime, string _name)
        {
            name = _name;
        }

        public string Name { get { return name; } }

        public P5Scalar Scalar
        {
            get { return scalar; }
            set { scalar = value; }
        }

        public P5Array Array
        {
            get { return array; }
            set { array = value; }
        }

        public P5Hash Hash
        {
            get { return hash; }
            set { hash = value; }
        }

        public P5Handle Handle
        {
            get { return handle; }
            set { handle = value; }
        }

        public P5Code Code
        {
            get { return code; }
            set { code = value; }
        }

        // IP5ScalarBody implementation
        public virtual IP5ScalarBody CloneBody(Runtime runtime)
        {
            var newBody = new P5TypeglobBody(runtime, name);

            newBody.scalar = scalar;
            newBody.array = array;
            newBody.hash = hash;
            newBody.handle = handle;
            newBody.code = code;

            return newBody;
        }

        public virtual string AsString(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual int AsInteger(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual double AsFloat(Runtime runtime) { throw new System.NotImplementedException(); }

        public virtual bool IsInteger(Runtime runtime) { return false; }
        public virtual bool IsString(Runtime runtime) { return true; }
        public virtual bool IsFloat(Runtime runtime) { return false; }

        public virtual bool AsBoolean(Runtime runtime)
        {
            return true;
        }

        public virtual int Length(Runtime runtime)
        {
            return AsString(runtime).Length;
        }

        public virtual string KeyString(Runtime runtime)
        {
            return AsString(runtime);
        }

        public virtual string ReferenceTypeString(Runtime runtime)
        {
            return "GLOB";
        }

        public virtual P5Scalar DereferenceScalar(Runtime runtime)
        {
            return scalar;
        }

        public virtual IP5Array DereferenceArray(Runtime runtime)
        {
            return array;
        }

        public virtual P5Hash DereferenceHash(Runtime runtime)
        {
            return hash;
        }

        public virtual P5Typeglob DereferenceGlob(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a GLOB reference");
        }

        public virtual P5Code DereferenceSubroutine(Runtime runtime)
        {
            return code;
        }

        public virtual P5Handle DereferenceHandle(Runtime runtime)
        {
            return handle;
        }

        public virtual int GetPos(Runtime runtime)
        {
            return pos;
        }

        public virtual int GetPos(Runtime runtime, out bool _pos_set)
        {
            _pos_set = pos_set;

            return pos;
        }

        public virtual void SetPos(Runtime runtime, int _pos, bool _pos_set)
        {
            pos = _pos;
            pos_set = _pos_set;
        }

        private int pos = -1;
        private bool pos_set = false;
        private P5Scalar scalar;
        private P5Array array;
        private P5Hash hash;
        private P5Handle handle;
        private P5Code code;
        private string name;
    }
}
