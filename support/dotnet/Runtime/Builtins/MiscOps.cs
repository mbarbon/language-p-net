using org.mbarbon.p.values;

using IEnumerable = System.Collections.IEnumerable;
using IEnumerator = System.Collections.IEnumerator;
using IList = System.Collections.IList;
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

        // Array assignment helpers

        public static object CloneObject(Runtime runtime, object value, int level)
        {
            var iany = value as IP5Any;
            var list = value as List<object>;

            if (iany != null)
                return iany.Clone(runtime, level);
            if (list != null)
                return CloneList(runtime, list, level);

            return value;
        }

        private static IEnumerator ScalarEnumerator(object value)
        {
            yield return value;
        }

        public static void PrepareArrayAssignment(Runtime runtime, object value, out IEnumerator iter, out int count)
        {
            // FIXME multiple dispatch
            P5Array a = value as P5Array;
            P5Hash h = value as P5Hash;
            P5Range r = value as P5Range;
            IList l = value as IList;

            if (a != null)
            {
                iter = a.GetEnumerator(runtime);
                count = a.GetCount(runtime);
            }
            else if (l != null)
            {
                iter = l.GetEnumerator();
                count = l.Count;
            }
            else if (r != null)
            {
                iter = r.GetEnumerator(runtime);
                count = r.GetCount();
            }
            else if (h != null)
            {
                iter = h.GetEnumerator(runtime);
                count = h.GetCount(runtime) * 2;
            }
            else
            {
                iter = ScalarEnumerator(value);
                count = 1;
            }
        }

        public static object AssignObjectIterator(Runtime runtime, object value, IEnumerator iter)
        {
            var iany = value as IP5Any;
            var list = value as List<object>;

            if (value == null)
                // TODO assign to undefined hash
                value = list = new List<object>();

            if (iany != null)
                iany.AssignIterator(runtime, iter);
            else if (list != null)
                AssignEnumerator(runtime, list, iter);

            return value;
        }
    }
}
