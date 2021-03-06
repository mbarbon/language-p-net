using Runtime = org.mbarbon.p.runtime.Runtime;
using System.Collections.Generic;

namespace org.mbarbon.p.values
{
    public class P5List : P5Array
    {
        public static readonly P5List EmptyList = new P5List(null);

        public P5List(Runtime runtime) : base(runtime)
        {
        }

        public P5List(Runtime runtime, int size) : base(runtime, size)
        {
        }

        public P5List(Runtime runtime, bool value) : base(runtime)
        {
            if (value)
                array.Add(new P5Scalar(runtime, 1));
        }

        public P5List(Runtime runtime, IP5Any value) :
            base(runtime, new IP5Any[] { value })
        {
        }

        public P5List(Runtime runtime, List<IP5Any> data) : base(runtime, data)
        {
        }

        public P5List(Runtime runtime, params IP5Any[] data) : base(runtime, data)
        {
        }

        public override P5Scalar AsScalar(Runtime runtime)
        {
            return array.Count == 0 ? new P5Scalar(runtime) : array[array.Count - 1].AsScalar(runtime);
        }

        public override bool IsDefined(Runtime runtime)
        {
            return AsScalar(runtime).IsDefined(runtime);
        }

        public override int AssignArray(Runtime runtime, IP5Value other)
        {
            // FIXME multiple dispatch
            P5Scalar s = other as P5Scalar;
            P5Array a = other as P5Array;
            IEnumerator<IP5Any> e = null;
            int c = 0;

            if (s != null)
            {
                e = s.GetEnumerator(runtime);
                c = 1;
            }
            else if (a != null)
            {
                e = a.GetEnumerator(runtime);
                c = a.GetCount(runtime);
            }

            foreach (var i in array)
                i.AssignIterator(runtime, e);

            return c;
        }

        public virtual P5List Slice(Runtime runtime, P5Array keys)
        {
            var res = new P5List(runtime);
            var list = new List<IP5Any>(keys.GetCount(runtime));
            bool found = false;

            foreach (var key in keys)
            {
                int i = key.AsInteger(runtime);

                if (i < array.Count)
                    found = true;
                list.Add(GetItemOrUndef(runtime, key, false));
            }

            if (found)
                res.SetArray(list);

            return res;
        }
    }

    public class P5LvalueList : P5List, IP5Enumerable
    {
        public P5LvalueList(Runtime runtime, params IP5Any[] data) :
            base(runtime, data)
        {
            flattened = false;
        }

        public P5LvalueList(Runtime runtime, List<IP5Any> data) :
            base(runtime, data)
        {
            flattened = false;
        }

        public override IP5Any Clone(Runtime runtime, int depth)
        {
            P5Array clone = new P5Array(runtime, array.Count);

            if (depth > 0)
            {
                foreach (var i in array)
                {
                    var enumerable = i as IP5Enumerable;

                    if (enumerable != null)
                        clone.PushFlatten(runtime, i);
                    else
                        clone.Push(runtime, i.Clone(runtime, depth - 1));
                }
            }
            else
                foreach (var i in array)
                    clone.PushFlatten(runtime, i);

            return clone;
        }

        public override P5List Slice(Runtime runtime, P5Array keys)
        {
            if (!flattened)
                Flatten(runtime);

            return base.Slice(runtime, keys);
        }

        public new IEnumerator<IP5Any> GetEnumerator(Runtime runtime)
        {
            if (!flattened)
                Flatten(runtime);

            return base.GetEnumerator();
        }

        private void Flatten(Runtime runtime)
        {
            var old = array;

            array = new List<IP5Any>();
            foreach (var i in old)
                PushFlatten(runtime, i);

            flattened = true;
        }

        bool flattened;
    }
}
