using org.mbarbon.p.values;

using IList = System.Collections.IList;
using System.Collections.Generic;
using IEnumerator = System.Collections.IEnumerator;
using IEnumerable = System.Collections.IEnumerable;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        public static object AssignHashItem(Runtime runtime, object item,
                                            object value)
        {
            if (!(item is P5Scalar))
                return value;

            P5Scalar scalar = item as P5Scalar;

            scalar.AssignObject(runtime, value);

            return item;
        }
    }
}