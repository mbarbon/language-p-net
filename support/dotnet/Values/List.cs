using Runtime = org.mbarbon.p.runtime.Runtime;
using Builtins = org.mbarbon.p.runtime.Builtins;
using System.Collections.Generic;
using IEnumerator = System.Collections.IEnumerator;
using IList = System.Collections.IList;

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

        public override object AssignArray(Runtime runtime, object other)
        {
            // FIXME multiple dispatch
            P5Scalar s = other as P5Scalar;
            P5Array a = other as P5Array;
            IList l = other as IList;
            IEnumerator e = null;
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
            else if (l != null)
            {
                e = l.GetEnumerator();
                c = l.Count;
            }

            foreach (var i in array)
                i.AssignIterator(runtime, e);

            return c;
        }

        public virtual object SliceList(Runtime runtime, IEnumerator keys)
        {
            var list = new List<object>();
            bool found = false;

            while (keys.MoveNext())
            {
                int idx = Builtins.ConvertToInteger(runtime, keys.Current);

                if (idx < array.Count)
                    found = true;
                list.Add(GetItemOrUndefInt(runtime, idx, false));
            }

            if (found)
                return list;

            return new List<object>();
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

        public override object SliceList(Runtime runtime, IEnumerator keys)
        {
            if (!flattened)
                Flatten(runtime);

            return base.SliceList(runtime, keys);
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
