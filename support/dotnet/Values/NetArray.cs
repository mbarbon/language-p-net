using Runtime = org.mbarbon.p.runtime.Runtime;
using NetGlue = org.mbarbon.p.runtime.NetGlue;
using Builtins = org.mbarbon.p.runtime.Builtins;
using System.Collections.Generic;
using System.Collections;

namespace org.mbarbon.p.values
{
    public class P5NetArray : AnyBase, IP5Array
    {
        public P5NetArray(System.Collections.IList _array, System.Type _type)
        {
            array = _array;
            type = _type;
        }

        // IP5Any
        public override P5Scalar AsScalar(Runtime runtime) { return new P5Scalar(runtime, array.Count); }
        public override string AsString(Runtime runtime) { return AsScalar(runtime).AsString(runtime); }
        public override int AsInteger(Runtime runtime) { return array.Count; }
        public override double AsFloat(Runtime runtime) { return array.Count; }
        public override bool AsBoolean(Runtime runtime) { return array.Count != 0; }

        public override int StringLength(Runtime runtime)
        {
            return AsString(runtime).Length;
        }

        public override int GetPos(Runtime runtime) { return -1; }
        public override int GetPos(Runtime runtime, out bool pos_set)
        {
            pos_set = false;

            return -1;
        }

        public override IP5Any AssignIterator(Runtime runtime, IEnumerator<IP5Any> e)
        {
            for (int i = 0; i < array.Count; ++i)
            {
                if (e.MoveNext())
                    array[i] = NetGlue.UnwrapValue<object>(runtime, e.Current);
                else
                    array[i] = null;
            }

            return this;
        }

        public override void Undef(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        public override IP5Any Clone(Runtime runtime, int depth)
        {
            return this;
        }

        public override IP5Any Localize(Runtime runtime)
        {
            throw new System.NotImplementedException();
        }

        // IP5Array
        public int GetCount(Runtime runtime) { return array.Count; }

        public IP5Any GetItemOrUndef(Runtime runtime, IP5Any index, bool create)
        {
            int idx = GetItemIndex(runtime, index.AsInteger(runtime), false);

            if (create)
                return new P5NetArrayItem(array, type, idx);
            else
                return NetGlue.WrapValue(array[idx]);
        }

        public IP5Any GetItem(Runtime runtime, int index)
        {
            return NetGlue.WrapValue(array[index]);
        }

        public int GetItemIndex(Runtime runtime, int i, bool create)
        {
            int idx = Builtins.GetItemIndex(runtime, array.Count, i, create);

            if (idx > array.Count)
                throw new System.NotImplementedException();

            return idx;
        }

        public P5List Slice(Runtime runtime, P5Array keys, bool create)
        {
            var res = new P5List(runtime, (List<IP5Any>) null);
            var list = new List<IP5Any>();

            foreach (var key in keys)
            {
                list.Add(GetItemOrUndef(runtime, key, create));
            }
            res.SetArray(list);

            return res;
        }

        public void PushFlatten(Runtime runtime, IP5Value value)
        {
            throw new System.NotImplementedException();
        }

        public P5Scalar PushList(Runtime runtime, P5Array items)
        {
            foreach (var item in items)
                array.Add(NetGlue.UnwrapValue(runtime, item, type));

            return new P5Scalar(runtime, array.Count);
        }

        public P5Scalar UnshiftList(Runtime runtime, P5Array items)
        {
            int i = 0;
            foreach (var item in items)
                array.Insert(i++, NetGlue.UnwrapValue(runtime, item, type));

            return new P5Scalar(runtime, array.Count);
        }

        public IP5Any PopElement(Runtime runtime)
        {
            if (array.Count == 0)
                return new P5Scalar(runtime);

            var res = array[array.Count - 1];
            array.RemoveAt(array.Count - 1);

            return NetGlue.WrapValue(res);
        }

        public IP5Any ShiftElement(Runtime runtime)
        {
            if (array.Count == 0)
                return new P5Scalar(runtime);

            var res = array[0];
            array.RemoveAt(0);

            return NetGlue.WrapValue(res);
        }

        public P5List Splice(Runtime runtime, int start, int length)
        {
            var res = new List<IP5Any>();

            // TODO _very_ inefficient, but IList does not have the
            // right interface
            for (int i = 0; i < length; ++i)
            {
                // TODO only in list context
                res.Add(NetGlue.WrapValue(array[start]));
                array.RemoveAt(start);
            }

            return new P5List(runtime, res);
        }

        public P5List Replace(Runtime runtime, int start, int length, IP5Any[] values)
        {
            // TODO duplicated
            var spliced = new List<IP5Any>();

            foreach (var i in values)
            {
                var a = i as P5Array;
                var h = i as P5Hash;
                IEnumerator<IP5Any> enumerator = null;

                if (h != null)
                    enumerator = ((P5Hash)h.Clone(runtime, 1)).GetEnumerator(runtime);
                else if (a != null)
                    enumerator = ((P5Array)a.Clone(runtime, 1)).GetEnumerator(runtime);

                if (enumerator != null)
                    while (enumerator.MoveNext())
                        spliced.Add(enumerator.Current);
                else
                    spliced.Add(i.Clone(runtime, 0));
            }

            var res = new List<IP5Any>();

            // TODO _very_ inefficient, but IList does not have the
            // right interface
            for (int i = 0; i < length; ++i)
            {
                // TODO only in list context
                res.Add(NetGlue.WrapValue(array[start]));
                array.RemoveAt(start);
            }

            for (int i = 0; i < spliced.Count; ++i)
                array.Insert(start + i, NetGlue.UnwrapValue(runtime, spliced[i], type));

            return new P5List(runtime, res);
        }

        public IP5Any LocalizeElement(Runtime runtime, int index)
        {
            throw new System.NotImplementedException();
        }

        public void RestoreElement(Runtime runtime, int index, IP5Any value)
        {
            throw new System.NotImplementedException();
        }

        // IP5Enumerable
        public IEnumerator<IP5Any> GetEnumerator(Runtime runtime)
        {
            return GetEnumerator();
        }

        // IEnumerable<IP5Any>
        public IEnumerator<IP5Any> GetEnumerator()
        {
            foreach (var i in array)
                yield return NetGlue.WrapValue(i);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // IP5Referrable
        public override void Bless(Runtime runtime, P5SymbolTable stash)
        {
            throw new System.NotImplementedException();
        }

        public override  bool IsBlessed(Runtime runtime)
        {
            return false;
        }

        public override  P5SymbolTable Blessed(Runtime runtime)
        {
            return null;
        }

        public override  string ReferenceTypeString(Runtime runtime)
        {
            return "ARRAY";
        }

        private System.Collections.IList array;
        private System.Type type;
    }

    public class P5NetArrayItem : P5ActiveScalar
    {
        public P5NetArrayItem(System.Collections.IList _array,
                              System.Type _type, int _index)
        {
            body = new P5NetArrayItemBody(_array, _type, _index);
        }
    }

    public class P5NetArrayItemBody : P5ActiveScalarBody
    {
        public P5NetArrayItemBody(System.Collections.IList _array,
                                  System.Type _type, int _index)
        {
            array = _array;
            type = _type;
            index = _index;
        }

        public override void Set(Runtime runtime, IP5Any other)
        {
            array[index] = NetGlue.UnwrapValue(runtime, other, type);
        }

        public override P5Scalar Get(Runtime runtime)
        {
            return NetGlue.WrapValue(array[index]);
        }

        private System.Collections.IList array;
        private System.Type type;
        private int index;
    }
}

