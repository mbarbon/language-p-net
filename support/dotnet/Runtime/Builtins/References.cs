using org.mbarbon.p.values;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
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
    }
}
