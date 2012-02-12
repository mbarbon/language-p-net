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
    }
}
