using org.mbarbon.p.values;

using IList = System.Collections.IList;
using System.Collections.Generic;
using IEnumerator = System.Collections.IEnumerator;
using IEnumerable = System.Collections.IEnumerable;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        // Push

        public static void PushFlattenIListObject(Runtime runtime, IList list, object value)
        {
            var p5e = value as IP5Enumerable;
            var e = value as IEnumerable;

            if (p5e != null)
                PushFlattenIListEnumerator(list, p5e.GetEnumerator(runtime));
            else if (e != null)
                PushFlattenIListEnumerator(list, e.GetEnumerator());
            else
                list.Add(value);
        }

        public static void PushFlattenIListEnumerator(IList list, IEnumerator values)
        {
            while (values.MoveNext())
                list.Add(values.Current);
        }

        public static object PushListIListObject(Runtime runtime, IList list, object value)
        {
            PushFlattenIListObject(runtime, list, value);

            return list.Count;
        }

        // Unshift

        public static void UnshiftFlattenIListObject(Runtime runtime, IList list, object value)
        {
            var p5e = value as IP5Enumerable;
            var e = value as IEnumerable;

            if (p5e != null)
                UnshiftFlattenIListEnumerator(list, p5e.GetEnumerator(runtime));
            else if (e != null)
                UnshiftFlattenIListEnumerator(list, e.GetEnumerator());
            else
                list.Insert(0, value);
        }

        public static void UnshiftFlattenIListEnumerator(IList list, IEnumerator values)
        {
            // TODO add efficient implementation for List<T>
            int index = 0;

            while (values.MoveNext())
                list.Insert(index++, values.Current);
        }

        public static object UnshiftListIListObject(Runtime runtime, IList list, object value)
        {
            UnshiftFlattenIListObject(runtime, list, value);

            return list.Count;
        }

        // Shift

        public static object ShiftElementIList(IList array)
        {
            if (array.Count == 0)
                return null;

            var e = array[0];
            array.RemoveAt(0);

            return e;
        }

        // Join

        public static string JoinEnumerator(Runtime runtime, IEnumerator iter)
        {
            var res = new System.Text.StringBuilder();
            bool first = true;

            iter.MoveNext();
            var sep = ConvertToString(runtime, iter.Current);

            while (iter.MoveNext())
            {
                if (!first)
                    res.Append(sep);
                first = false;
                res.Append(ConvertToString(runtime, iter.Current));
            }

            return res.ToString();
        }

        // Index/slice

        public static int GetItemIndex(int count, int i, bool create)
        {
            if (i < 0 && -i > count)
                return -1;
            if (i < 0)
                return count + i;

            if (i < count)
                return i;
            if (create)
                return i;
            else
                return -2;
        }

        public static void GetRangeOffsets(int count, ref int offset,
                                           ref int length)
        {
            if (offset < 0)
                offset = count + offset;

            if (length < 0)
                length = count + length - offset;
        }

        public static int GetRangeOffsets(int count, ref int offset)
        {
            if (offset < 0)
                offset = count + offset;

            int length = count - offset;
            if (length < 0)
                length = count + length - offset;

            return length;
        }

        public static object GetListItemOrUndefInt(List<object> array, int index, bool create)
        {
            int idx = GetItemIndex(array.Count, index, create);

            if (create && idx >= array.Count)
                while (array.Count <= idx)
                    array.Add(null);

            if (idx == -1)
            {
                if (create)
                    throw new System.Exception("Modification of non-creatable array value attempted, subscript " + index);
                else
                    return null;
            }
            else if (idx == -2)
                return null;

            return array[idx];
        }

        public static object AssignArrayItem(Runtime runtime, object item,
                                             object value)
        {
            if (!(item is P5Scalar))
                return value;

            P5Scalar scalar = item as P5Scalar;

            scalar.AssignObject(runtime, value);

            return item;
        }

        public static void AssignEnumerator(Runtime runtime,
                                            List<object> array,
                                            IEnumerator iter)
        {
            array.Clear();

            while (iter.MoveNext())
            {
                var iany = iter.Current as IP5Any;

                if (iany != null)
                    array.Add(iany.Clone(runtime, 0));
                else
                    array.Add(iter.Current);
            }
        }

        public static void AssignEnumerator(Runtime runtime,
                                            List<object> array,
                                            IEnumerator<object> iter)
        {
            array.Clear();

            while (iter.MoveNext())
            {
                IP5Any current = iter.Current as IP5Any;

                if (current != null)
                    array.Add(current.Clone(runtime, 0));
                else
                    array.Add(iter.Current);
            }
        }

        public static object CloneList(Runtime runtime, List<object> array,
                                       int depth)
        {
            List<object> clone = null;

            if (depth == 0)
            {
                clone = new List<object>(array);
            }
            else
            {
                clone = new List<object>(array.Count);

                foreach (var i in array)
                {
                    var iany = i as IP5Any;

                    if (iany != null)
                        clone.Add(iany.Clone(runtime, depth - 1));
                    else
                        clone.Add(i);
                }
            }

            return clone;
        }

        public static object AssignArrayList(Runtime runtime,
                                             List<object> array,
                                             object other)
        {
            // FIXME multiple dispatch
            P5Scalar s = other as P5Scalar;
            P5Array a = other as P5Array;
            P5Hash h = other as P5Hash;
            P5NetArray na = other as P5NetArray;
            List<object> lo = other as List<object>;

            if (lo != null)
            {
                AssignEnumerator(runtime, array, lo.GetEnumerator());

                return lo.Count;
            }
            else if (s != null)
            {
                array.Clear();
                array.Add(s.Clone(runtime, 1));

                return 1;
            }
            else if (h != null)
            {
                AssignEnumerator(runtime, array, h.GetEnumerator(runtime));

                return h.GetCount(runtime) * 2;
            }
            else if (a != null)
            {
                AssignEnumerator(runtime, array, a.GetEnumerator(runtime));

                return a.GetCount(runtime);
            }
            else if (na != null)
            {
                AssignEnumerator(runtime, array, na.GetEnumerator(runtime));

                return na.GetCount(runtime);
            }

            return 0;
        }

        public static object SliceArrayListEnumerator(Runtime runtime, List<object> array, IEnumerator keys, bool create)
        {
            var list = new List<object>();

            while (keys.MoveNext())
            {
                int idx = Builtins.ConvertToInteger(runtime, keys.Current);

                list.Add(GetListItemOrUndefInt(array, idx, create));
            }

            return list;
        }

        public static object SliceListListEnumerator(Runtime runtime, List<object> array, IEnumerator keys)
        {
            var list = new List<IP5Any>();
            bool found = false;

            while (keys.MoveNext())
            {
                int idx = Builtins.ConvertToInteger(runtime, keys.Current);

                if (idx < array.Count)
                    found = true;
                list.Add(UpgradeScalar(
                             runtime, GetListItemOrUndefInt(array, idx, false)));
            }

            // TODO return a List<object> subclass
            if (found)
                return new P5List(runtime, list);

            return new P5List(runtime);
        }

        public static object Sort(Runtime runtime, List<object> array)
        {
            var list = new List<object>(array);

            list.Sort(delegate(object a, object b)
                      {
                          return string.Compare(
                              Builtins.ConvertToString(runtime, a),
                              Builtins.ConvertToString(runtime, b));
                      });

            return list;
        }

        public static object SpliceAll(Runtime runtime, List<object> array,
                                       int offset)
        {
            int count = GetRangeOffsets(array.Count, ref offset);

            return SpliceCount(runtime, array, offset, count);
        }

        public static object SpliceCount(Runtime runtime, List<object> array,
                                         int offset, int count)
        {
            GetRangeOffsets(array.Count, ref offset, ref count);

            // TODO void/scalar context
            var res = array.GetRange(offset, count);

            array.RemoveRange(offset, count);

            return res;
        }

        public static object Replace(Runtime runtime, List<object> array,
                                     int offset, int count,
                                     object[] values)
        {
            GetRangeOffsets(array.Count, ref offset, ref count);

            var spliced = new List<object>();

            // TODO merge with P5Array/NetArray
            foreach (var i in values)
            {
                var p5enumerable = i as IP5Enumerable;
                var enumerable = i as IEnumerable;
                var iany = i as IP5Any;
                IEnumerator enumerator = null;

                if (p5enumerable != null)
                    enumerator = p5enumerable.GetEnumerator(runtime);
                else if (enumerable != null)
                    enumerator = enumerable.GetEnumerator();

                if (enumerator != null)
                {
                    while (enumerator.MoveNext())
                    {
                        var iany2 = enumerator.Current as IP5Any;

                        if (iany2 != null)
                            spliced.Add(iany2.Clone(runtime, 0));
                        else
                            spliced.Add(enumerator.Current);
                    }
                }
                else if (iany != null)
                    spliced.Add(iany.Clone(runtime, 0));
                else
                    spliced.Add(i);
            }

            // TODO void/scalar context
            var res = array.GetRange(offset, count);

            // TODO optimize
            array.RemoveRange(offset, count);
            array.InsertRange(offset, spliced);

            return res;
        }
    }
}
