using org.mbarbon.p.values;

using IEnumerable = System.Collections.IEnumerable;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        // Concatenation

        public static object ConcatenateScalarStringAssign(Runtime runtime, P5Scalar left, string right)
        {
            return left.ConcatAssign(runtime, right);
        }

        public static object ConcatenateScalarString(Runtime runtime, P5Scalar left, string right)
        {
            return left.AsString(runtime) + right;
        }

        // String length

        public static object StringLengthIP5Any(Runtime runtime, IP5Any scalar)
        {
            return scalar.AsString(runtime).Length;
        }

        public static object StringLengthObject(Runtime runtime, object obj)
        {
            return obj.ToString().Length;
        }

        // Chr, Ord, Uc, Lc

        public static object Ord(Runtime runtime, string value)
        {
            return value.Length > 0 ? (int)value[0] : 0;
        }

        public static object Chr(Runtime runtime, int value)
        {
            return new string((char)value, 1);
        }

        public static object Uppercase(Runtime runtime, string value)
        {
            return value.ToUpper();
        }

        public static object Lowercase(Runtime runtime, string value)
        {
            return value.ToLower();
        }
    }
}