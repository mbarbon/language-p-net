using org.mbarbon.p.runtime;
using System.Collections.Generic;
using IEnumerator = System.Collections.IEnumerator;

namespace org.mbarbon.p.values
{
    public interface IP5Hash : IP5Any, IP5Referrable, IP5Enumerable
    {
        object GetItemOrUndefString(Runtime runtime, string key, bool create);
    }

    public class P5Hash : IP5Hash
    {
        public P5Hash(Runtime runtime)
        {
            hash = new Dictionary<string, IP5Any>();
        }

        public P5Hash(Runtime runtime, IP5Enumerable items) : this(runtime)
        {
            AssignIterator(runtime, items.GetEnumerator(runtime));
        }

        public int GetCount(Runtime runtime)
        {
            return hash.Count;
        }

        public virtual void Undef(Runtime runtime)
        {
            if (hash.Count != 0)
            {
                hash.Clear();
                iterator = null;
            }
        }

        internal void SetItem(Runtime runtime, string key, IP5Any value)
        {
            hash[key] = value;
        }

        public IP5Any GetItem(Runtime runtime, string key) { return hash[key]; }

        public object GetItemOrUndefString(Runtime runtime, string key, bool create)
        {
            IP5Any v = null;

            if (hash.TryGetValue(key, out v))
                return v;
            if (create)
            {
                v = new P5Scalar(runtime);
                hash[key] = v;

                return v;
            }

            return null;
        }

        public IP5Any GetItemOrUndef(Runtime runtime, IP5Any key, bool create)
        {
            string k = key.AsScalar(runtime).KeyString(runtime);
            IP5Any v = null;

            if (hash.TryGetValue(k, out v))
                return v;
            if (create)
            {
                v = new P5Scalar(runtime);
                hash[k] = v;

                return v;
            }

            return new P5Scalar(runtime);
        }

        public object SliceArray(Runtime runtime, IEnumerator keys, bool create)
        {
            var list = new List<object>();

            while (keys.MoveNext())
            {
                string key = Builtins.ConvertToKeyString(runtime, keys.Current);

                list.Add(GetItemOrUndefString(runtime, key, create));
            }

            return list;
        }

        public bool ExistsKey(Runtime runtime, string key)
        {
            return hash.ContainsKey(key);
        }

        public IP5Any Delete(Runtime runtime, IP5Any key)
        {
            string k = key.AsScalar(runtime).KeyString(runtime);
            IP5Any value;
            bool has_value = hash.TryGetValue(k, out value);

            if (has_value)
            {
                hash.Remove(k);

                return value;
            }
            else
                return new P5Scalar(runtime);
        }

        public virtual P5Scalar AsScalar(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual int AsInteger(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual double AsFloat(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual string AsString(Runtime runtime) { throw new System.NotImplementedException(); }
        public virtual bool AsBoolean(Runtime runtime) { return hash.Count != 0; }
        public virtual bool IsDefined(Runtime runtime) { return hash.Count != 0; }

        public virtual P5Handle DereferenceHandle(Runtime runtime)
        {
            throw new System.NotImplementedException("No DereferenceHandle for P5Hash");
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
            P5Array a = other as P5Array;
            P5Hash h = other as P5Hash;
            List<object> l = other as List<object>;

            iterator = null;

            if (a != null)
            {
                AssignIterator(runtime, a.GetEnumerator(runtime));

                return a.GetCount(runtime);
            }
            else if (h != null)
            {
                hash.Clear();
                foreach (var e in h.hash)
                    hash[e.Key] = e.Value.Clone(runtime, 0);

                return h.GetCount(runtime) * 2;
            }
            else if (l != null)
            {
                AssignIterator(runtime, l.GetEnumerator());

                return l.Count;
            }

            return 0;
        }

        public virtual IP5Any AssignIterator(Runtime runtime, IEnumerator e)
        {
            hash.Clear();
            while (e.MoveNext())
            {
                string k = Builtins.ConvertToKeyString(runtime, e.Current);
                e.MoveNext();
                IP5Any v = Builtins.UpgradeScalar(runtime, e.Current);

                hash[k] = v.Clone(runtime, 0);
            }

            iterator = null;

            return this;
        }

        public IEnumerator GetEnumerator(Runtime runtime)
        {
            foreach (var i in hash)
            {
                yield return new P5Scalar(runtime, i.Key);
                yield return i.Value;
            }
        }

        public P5List Keys(Runtime runtime)
        {
            iterator = null;

            var data = new List<IP5Any>(hash.Count);

            foreach (var k in hash.Keys)
                data.Add(new P5Scalar(runtime, k));

            return new P5List(runtime, data);
        }

        public P5List Values(Runtime runtime)
        {
            iterator = null;

            var data = new List<IP5Any>(hash.Count);

            foreach (var v in hash.Values)
                data.Add(v);

            return new P5List(runtime, data);
        }

        private IEnumerator<string> KeyIterator()
        {
            return new List<string>(hash.Keys).GetEnumerator();
        }

        public bool NextKey(Runtime runtime, out P5Scalar key, out P5Scalar value)
        {
            if (iterator == null)
                iterator = KeyIterator();

            if (!iterator.MoveNext())
            {
                key = value = null;
                iterator.Reset();

                return false;
            }

            key = new P5Scalar(runtime, iterator.Current);
            value = hash[iterator.Current] as P5Scalar;

            return true;
        }

        public virtual IP5Any Clone(Runtime runtime, int depth)
        {
            P5Hash clone = new P5Hash(runtime);

            foreach (var e in hash)
            {
                if (depth == 0)
                    clone.hash.Add(e.Key, e.Value);
                else
                    clone.hash.Add(e.Key, e.Value.Clone(runtime, depth - 1));
            }

            return clone;
        }

        public virtual IP5Any Localize(Runtime runtime)
        {
            return new P5Hash(runtime);
        }

        public IP5Any LocalizeElement(Runtime runtime, string key)
        {
            IP5Any v;

            hash.TryGetValue(key, out v); // ok to return null
            hash[key] = new P5Scalar(runtime);

            return v;
        }

        public virtual void RestoreElement(Runtime runtime, string key, IP5Any value)
        {
            if (value == null)
                hash.Remove(key);
            else
                hash[key] = value;
        }

        public virtual string ReferenceTypeString(Runtime runtime)
        {
            return "HASH";
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

        private P5SymbolTable blessed;
        protected Dictionary<string, IP5Any> hash;
        private IEnumerator<string> iterator;
    }
}
