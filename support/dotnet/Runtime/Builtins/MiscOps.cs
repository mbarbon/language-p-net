using org.mbarbon.p.values;

using IEnumerable = System.Collections.IEnumerable;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        // Repeat

        public static object RepeatArray(IEnumerable elements, int count)
        {
            var temp = new List<object>();
            var list = new List<object>(temp.Count * count);

            foreach (var item in elements)
                temp.Add(item);
            for (int i = 0; i < count; ++i)
                list.AddRange(temp);

            return list;
        }

        public static object RepeatScalar(string value, int count)
        {
            var str = new System.Text.StringBuilder(value.Length * count);

            for (int i = 0; i < count; ++i)
                str.Append(value);

            return str.ToString();
        }

        // String concatenation

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
    }
}
