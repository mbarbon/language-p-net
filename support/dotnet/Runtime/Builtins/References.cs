using org.mbarbon.p.values;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        // Reference constructors

        public static P5Scalar AnonymousArray(Runtime runtime, List<object> list)
        {
            var result = new P5Array(runtime, list.Count);

            result.AssignIterator(runtime, list.GetEnumerator());

            return new P5Scalar(runtime, result);
        }

        public static P5Scalar AnonymousHash(Runtime runtime, List<object> list)
        {
            var result = new P5Hash(runtime);

            result.AssignIterator(runtime, list.GetEnumerator());

            return new P5Scalar(runtime, result);
        }

        // Reference type

        public static string ReferenceType(object value)
        {
            if (value != null)
                return "";

            return null;
        }

        // Dereference

        public static P5Typeglob SymbolicReference(Runtime runtime, string name, bool create)
        {
            // TODO strict check

            if (name.IndexOf("::") == -1 && name.IndexOf("'") == -1)
            {
                name = runtime.Package + "::" + name;
            }

            // TODO must handle punctuation variables and other special cases
            var glob = runtime.SymbolTable.GetGlob(runtime, name, create);

            return glob;
        }

        public static P5Typeglob DereferenceGlob(Runtime runtime, string name)
        {
            return SymbolicReference(runtime, name, true);
        }

        public static P5Scalar DereferenceScalar(Runtime runtime, string name)
        {
            var glob = SymbolicReference(runtime, name, true);

            if (glob == null)
                return null;
            if (glob.Scalar != null)
                return glob.Scalar;

            return glob.Scalar = new P5Scalar(runtime);
        }

        public static P5Array DereferenceArray(Runtime runtime, string name)
        {
            var glob = SymbolicReference(runtime, name, true);

            if (glob == null)
                return null;
            if (glob.Array != null)
                return glob.Array;

            return glob.Array = new P5Array(runtime);
        }

        public static P5Code DereferenceCode(Runtime runtime, string name)
        {
            var glob = SymbolicReference(runtime, name, true);

            if (glob == null)
                return null;
            if (glob.Code != null)
                return glob.Code;

            return glob.Code = new P5Code(name, null);
        }

        public static P5Handle DereferenceHandle(Runtime runtime, string name)
        {
            var glob = SymbolicReference(runtime, name, true);

            if (glob == null)
                return null;

            return glob.Handle;
        }

        public static P5Hash DereferenceHash(Runtime runtime, string name)
        {
            if (name.EndsWith("::"))
            {
                var pack = runtime.SymbolTable.GetPackage(runtime, name.Substring(0, name.Length - 2), true);

                return pack;
            }

            var glob = SymbolicReference(runtime, name, true);

            if (glob == null)
                return null;
            if (glob.Hash != null)
                return glob.Hash;

            return glob.Hash = new P5Hash(runtime);
        }
    }
}
