using org.mbarbon.p.values;
using System.Collections.Generic;
using IList = System.Collections.IList;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        public static IP5Any LocalizeArrayElementIP5Array(Runtime runtime, IP5Array array, int idx, ref SavedValue state)
        {
            int index = array.GetItemIndex(runtime, idx, true);
            var saved = array.LocalizeElement(runtime, index);

            state.container = array;
            state.int_key = index;
            state.value = saved;

            return array.GetItem(runtime, index);
        }

        public static object LocalizeArrayElementIList(Runtime runtime, IList array, int idx, ref SavedValue state)
        {
            int index = GetItemIndex(array.Count, idx, true);

            if (index == -1)
                throw new System.Exception("Modification of non-creatable array value attempted, subscript " + index);

            state.container = array;
            state.int_key = index;
            state.value = array[index];

            array[index] = null;

            return null;
        }

        // TODO remove ref so it can be used in a CallSite
        public static object LocalizeArrayElement(Runtime runtime, object array, int idx, ref SavedValue state)
        {
            var p5array = array as IP5Array;
            var ilist = array as IList;

            if (p5array != null)
                return LocalizeArrayElementIP5Array(runtime, p5array, idx, ref state);
            else if (ilist != null)
                return LocalizeArrayElementIList(runtime, ilist, idx, ref state);
            else
                throw new System.Exception("Unhandled container type: " + state.container.GetType());
        }

        public static void RestoreArrayElement(Runtime runtime, ref SavedValue state)
        {
            if (state.container == null)
                return;

            var list = state.container as List<object>;
            var p5array = state.container as P5Array;

            if (list != null)
                list[state.int_key] = state.value;
            else if (p5array != null)
                p5array.RestoreElement(
                    runtime, state.int_key, state.value as IP5Any);
            else
                throw new System.Exception("Unhandled container type: " + state.container.GetType());

            state.container = null;
            state.str_key = null;
            state.value = null;
        }

        public static IP5Any LocalizeHashElementP5Hash(Runtime runtime, P5Hash hash, string index, ref SavedValue state)
        {
            var saved = hash.LocalizeElement(runtime, index);
            var new_value = new P5Scalar(runtime);

            state.container = hash;
            state.str_key = index;
            state.value = saved;

            hash.SetItem(runtime, index, new_value);

            return new_value;
        }

        // TODO remove ref so it can be used in a CallSite
        public static object LocalizeHashElement(Runtime runtime, object hash, string index, ref SavedValue state)
        {
            var p5hash = hash as P5Hash;

            if (p5hash != null)
                return LocalizeHashElementP5Hash(runtime, p5hash, index, ref state);
            else
                throw new System.Exception("Unhandled container type: " + state.container.GetType());
        }

        public static void RestoreHashElement(Runtime runtime, ref SavedValue state)
        {
            if (state.container == null)
                return;

            var p5hash = state.container as P5Hash;

            if (p5hash != null)
                p5hash.RestoreElement(
                    runtime, state.str_key, state.value as IP5Any);
            else
                throw new System.Exception("Unhandled container type: " + state.container.GetType());

            state.container = null;
            state.str_key = null;
            state.value = null;
        }
    }
}