using Runtime = org.mbarbon.p.runtime.Runtime;
using Opcode = org.mbarbon.p.runtime.Opcode;
using Builtins = org.mbarbon.p.runtime.Builtins;
using System.Collections.Generic;
using System.Collections;

namespace org.mbarbon.p.values
{
    public interface IP5Array : IP5Any, IEnumerable<IP5Any>, IP5Enumerable
    {
        int GetCount(Runtime runtime);

        IP5Any GetItemOrUndef(Runtime runtime, IP5Any index, bool create);
        object GetItemOrUndefInt(Runtime runtime, int index, bool create);
        IP5Any GetItem(Runtime runtime, int i);
        int GetItemIndex(Runtime runtime, int i, bool create);

        void PushFlatten(Runtime runtime, IP5Value value);
        P5Scalar PushList(Runtime runtime, IEnumerable items);
        P5Scalar UnshiftList(Runtime runtime, IEnumerable items);
        IP5Any PopElement(Runtime runtime);
        IP5Any ShiftElement(Runtime runtime);
/*
        P5List Splice(Runtime runtime, int start, int length);
        P5List Replace(Runtime runtime, int start, int length,
                       object[] values);
*/
        IP5Any LocalizeElement(Runtime runtime, int index);
        void RestoreElement(Runtime runtime, int index, IP5Any value);
    }

    public class P5Array : IP5Array
    {
        public P5Array(Runtime runtime)
        {
            array = new List<IP5Any>();
        }

        public P5Array(Runtime runtime, int size)
        {
            array = new List<IP5Any>(size);
        }

        public P5Array(Runtime runtime, params IP5Any[] data)
        {
            array = new List<IP5Any>(data);
        }

        public P5Array(Runtime runtime, List<IP5Any> data)
        {
            array = data;
        }

        public P5Array(Runtime runtime, IP5Enumerable items) : this(runtime)
        {
            AssignIterator(runtime, items.GetEnumerator(runtime));
        }

        public virtual void Undef(Runtime runtime)
        {
            if (array.Count != 0)
                array = new List<IP5Any>();
        }

        public void PushFlatten(Runtime runtime, IP5Value value)
        {
            var v = value as IP5Enumerable;

            if (v != null)
            {
                var iter = v.GetEnumerator(runtime);
                while (iter.MoveNext())
                    array.Add(Builtins.UpgradeScalar(runtime, iter.Current));
            }
            else
                array.Add(Builtins.UpgradeScalar(runtime, value));
        }

        protected void PushFlatten(Runtime runtime, IP5Value[] data)
        {
            foreach (var i in data)
                PushFlatten(runtime, i);
        }

        public int GetCount(Runtime runtime) { return array.Count; }
        public IP5Any GetItem(Runtime runtime, int i) { return array[i]; }

        public int GetItemIndex(Runtime runtime, int i, bool create)
        {
            int idx = Builtins.GetItemIndex(array.Count, i, create);

            if (create && idx >= array.Count)
                while (array.Count <= idx)
                    array.Add(new P5Scalar(runtime));

            return idx;
        }

        public object GetItemOrUndefInt(Runtime runtime, int index, bool create)
        {
            int i = GetItemIndex(runtime, index, create);

            if (i == -1)
            {
                if (create)
                    throw new System.Exception("Modification of non-creatable array value attempted, subscript " + index);
                else
                    return null;
            }
            else if (i == -2)
                return null;

            return array[i];
        }

        public IP5Any GetItemOrUndef(Runtime runtime, IP5Any index, bool create)
        {
            int i = GetItemIndex(runtime, index.AsInteger(runtime), create);

            if (i == -1)
            {
                if (create)
                    throw new System.Exception("Modification of non-creatable array value attempted, subscript " + i.ToString());
                else
                    return new P5Scalar(runtime);
            }
            else if (i == -2)
                return new P5Scalar(runtime);

            return array[i];
        }

        public object SliceArray(Runtime runtime, IEnumerator keys, bool create)
        {
            var list = new List<object>();

            while (keys.MoveNext())
            {
                int idx = Builtins.ConvertToInteger(runtime, keys.Current);

                list.Add(GetItemOrUndefInt(runtime, idx, create));
            }

            return list;
        }

        public IP5Any Exists(Runtime runtime, IP5Any index)
        {
            int i = index.AsInteger(runtime);

            return new P5Scalar(runtime, (i >= 0 && i < array.Count) || (i < 0 && -i < array.Count));
        }

        public IEnumerator GetEnumerator(Runtime runtime)
        {
            return array.GetEnumerator();
        }

        // implement both System.Collections.Generic.IEnumerable<T>
        // and System.Collections.IEnumerable
        public IEnumerator<IP5Any> GetEnumerator()
        {
            return array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return array.GetEnumerator();
        }

        public virtual void Push(Runtime runtime, IP5Any item)
        {
            array.Add(item);
        }

        public virtual P5Scalar PushList(Runtime runtime, IEnumerable items)
        {
            foreach (var item in items)
            {
                var iany = item as IP5Any;

                if (iany != null)
                    array.Add(iany.Clone(runtime, 0));
                else
                    array.Add(Builtins.UpgradeScalar(runtime, item));
            }

            return new P5Scalar(runtime, array.Count);
        }

        public virtual IP5Any PopElement(Runtime runtime)
        {
            if (array.Count == 0)
                return new P5Scalar(runtime);
            int last = array.Count - 1;
            var e = array[last];

            array.RemoveAt(last);

            return e;
        }

        public virtual P5Scalar UnshiftList(Runtime runtime, IEnumerable items)
        {
            // TODO pre-alloc using items.GetCount(runtime) if available
            var new_array = new List<IP5Any>(array.Count);

            foreach (var item in items)
            {
                var iany = item as IP5Any;

                if (iany != null)
                    new_array.Add(iany.Clone(runtime, 0));
                else
                    new_array.Add(Builtins.UpgradeScalar(runtime, item));
            }
            new_array.AddRange(array);

            array = new_array;

            return new P5Scalar(runtime, array.Count);
        }

        public virtual IP5Any ShiftElement(Runtime runtime)
        {
            if (array.Count == 0)
                return new P5Scalar(runtime);
            var e = array[0];

            array.RemoveAt(0);

            return e;
        }

        public virtual P5Scalar AsScalar(Runtime runtime) { return new P5Scalar(runtime, array.Count); }
        public virtual string AsString(Runtime runtime) { return AsScalar(runtime).AsString(runtime); }
        public virtual int AsInteger(Runtime runtime) { return array.Count; }
        public virtual double AsFloat(Runtime runtime) { return array.Count; }
        public virtual bool AsBoolean(Runtime runtime) { return array.Count != 0; }
        public virtual bool IsDefined(Runtime runtime) { return array.Count != 0; }

        public virtual P5Handle DereferenceHandle(Runtime runtime)
        {
            throw new System.NotImplementedException("No DereferenceHandle for P5Array");
        }

        public virtual int GetPos(Runtime runtime)
        {
            return -1;
        }

        public virtual int GetPos(Runtime runtime, out bool pos_set)
        {
            pos_set = false;

            return -1;
        }

        public virtual object AssignArray(Runtime runtime, object other)
        {
            // FIXME multiple dispatch
            P5Scalar s = other as P5Scalar;
            IP5Array a = other as IP5Array;
            P5Hash h = other as P5Hash;
            List<object> l = other as List<object>;

            if (s != null)
            {
                array = new List<IP5Any>(1);
                array.Add(s.Clone(runtime, 1));

                return 1;
            }
            else if (h != null)
            {
                AssignIterator(runtime, h.GetEnumerator(runtime));

                return h.GetCount(runtime) * 2;
            }
            else if (a != null)
            {
                AssignIterator(runtime, a.GetEnumerator(runtime));

                return a.GetCount(runtime);
            }
            else if (l != null)
            {
                AssignIterator(runtime, l.GetEnumerator());

                return l.Count;
            }
            else
            {
                array = new List<IP5Any>(1);
                array.Add(Builtins.UpgradeScalar(runtime, other));

                return 1;
            }
        }

        public virtual IP5Any AssignIterator(Runtime runtime, IEnumerator iter)
        {
            array = new List<IP5Any>();

            while (iter.MoveNext())
            {
                var iany = iter.Current as IP5Any;

                if (iany != null)
                    array.Add(iany.Clone(runtime, 0));
                else
                    array.Add(Builtins.UpgradeScalar(runtime, iter.Current));
            }

            return this;
        }

        public virtual IP5Any Clone(Runtime runtime, int depth)
        {
            P5Array clone = new P5Array(runtime, (List<IP5Any>) null);

            if (depth == 0)
            {
                clone.SetArray(new List<IP5Any>(array));
            }
            else
            {
                var list = new List<IP5Any>(array.Count);

                foreach (var i in array)
                    list.Add(i.Clone(runtime, depth - 1));

                clone.SetArray(list);
            }

            return clone;
        }

        public virtual IP5Any Localize(Runtime runtime)
        {
            return new P5Array(runtime);
        }

        public virtual IP5Any LocalizeElement(Runtime runtime, int index)
        {
            var value = array[index];
            var new_value = new P5Scalar(runtime);

            array[index] = new_value;

            return value;
        }

        public virtual void RestoreElement(Runtime runtime, int index, IP5Any value)
        {
            array[index] = value;
        }

        public virtual string ReferenceTypeString(Runtime runtime)
        {
            return "ARRAY";
        }

        public virtual P5Scalar DereferenceScalar(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual IP5Array DereferenceArray(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Hash DereferenceHash(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Typeglob DereferenceGlob(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Code DereferenceSubroutine(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Scalar VivifyScalar(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual IP5Array VivifyArray(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        public virtual P5Hash VivifyHash(Runtime runtime)
        {
            throw new System.InvalidOperationException("Not a reference");
        }

        internal void SetArray(List<IP5Any> a)
        {
            array = a;
        }

        public virtual void Bless(Runtime runtime, P5SymbolTable stash)
        {
            blessed = stash;
        }

        public virtual bool IsBlessed(Runtime runtime)
        {
            return blessed != null;
        }

        public virtual P5Code FindMethod(Runtime runtime, string method)
        {
            return blessed.FindMethod(runtime, method);
        }

        public virtual P5SymbolTable Blessed(Runtime runtime)
        {
            return blessed;
        }

        public object CallMethod(Runtime runtime, Opcode.ContextValues context,
                                 string method)
        {
            var invocant = array[0] as P5Scalar;

            return invocant.CallMethod(runtime, context, method, this);
        }

        public object CallMethodIndirect(Runtime runtime, Opcode.ContextValues context,
                                         P5Scalar method)
        {
            var pmethod = method.IsReference(runtime) ? method.Dereference(runtime) as P5Code : null;

            if (pmethod != null)
                return pmethod.Call(runtime, context, this);

            return CallMethod(runtime, context, method.AsString(runtime));
        }

        public P5List Repeat(Runtime runtime, IP5Any c)
        {
            int count = c.AsInteger(runtime);
            var list = new List<IP5Any>();

            for (int i = 0; i < count; ++i)
                list.AddRange(array);

            return new P5List(runtime, list);
        }

        public P5List SpliceAll(Runtime runtime, int offset)
        {
            int count = Builtins.GetRangeOffsets(GetCount(runtime), ref offset);

            return SpliceCount(runtime, offset, count);
        }

        public P5List SpliceCount(Runtime runtime, int offset, int count)
        {
            Builtins.GetRangeOffsets(GetCount(runtime), ref offset, ref count);

            // TODO void/scalar context
            var res = array.GetRange(offset, count);

            array.RemoveRange(offset, count);

            return new P5List(runtime, res);
        }

        public P5List Replace(Runtime runtime, int start, int length,
                              object[] values)
        {
            Builtins.GetRangeOffsets(GetCount(runtime), ref start, ref length);

            var spliced = new List<IP5Any>();

            // TODO merge in builtins after changing array into List<object>
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
                            spliced.Add(Builtins.UpgradeScalar(
                                            runtime, enumerator.Current));
                    }
                }
                else if (iany != null)
                    spliced.Add(iany.Clone(runtime, 0));
                else
                    spliced.Add(Builtins.UpgradeScalar(runtime, i));
            }

            var res = array.GetRange(start, length);

            // TODO optimize
            array.RemoveRange(start, length);
            array.InsertRange(start, spliced);

            return new P5List(runtime, res);
        }

        public object Sort(Runtime runtime)
        {
            var list = new List<IP5Any>(array);

            list.Sort(delegate(IP5Any a, IP5Any b)
                      {
                          return string.Compare(
                              a.AsString(runtime),
                              b.AsString(runtime));
                      });

            return new P5List(runtime, list);
        }

        private P5SymbolTable blessed;
        protected List<IP5Any> array;
    }
}
