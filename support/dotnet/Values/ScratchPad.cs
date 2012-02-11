using System.Collections.Generic;

using org.mbarbon.p.runtime;

namespace org.mbarbon.p.values
{
    public class P5ScratchPad : List<object>
    {
        public P5ScratchPad()
        {
            Lexicals = new List<LexicalInfo>();
        }

        public P5ScratchPad(IEnumerable<object> values, List<LexicalInfo> lexicals)
            : base(values)
        {
            Lexicals = lexicals;
        }

        public static P5ScratchPad CreateMainPad(Runtime runtime,
                                                 IList<LexicalInfo> lexicals,
                                                 P5ScratchPad main)
        {
            foreach (var lex in lexicals)
            {
                main.AddValue(lex);
                if (!lex.InPad)
                    continue;
                main.GetOrCreateValue(runtime, lex.Slot, lex.Index);
            }

            return main;
        }

        public static P5ScratchPad CreateSubPad(Runtime runtime,
                                                IList<LexicalInfo> lexicals,
                                                P5ScratchPad main)
        {
            var pad = new P5ScratchPad();
            int from_outside = 0;

            foreach (var lex in lexicals)
            {
                if (!lex.InPad)
                    continue;
                pad.AddValue(lex);

                if (lex.FromMain)
                {
                    while (pad.Count <= lex.Index)
                        pad.Add(null);
                    pad[lex.Index] = main.GetOrCreateValue(runtime, lex.Slot,
                                                           lex.OuterIndex);
                }

                if (lex.OuterIndex != -1)
                    from_outside += 1;
            }

            if (pad.Lexicals.Count == 0)
                return null;
            pad.Complete = pad.Lexicals.Count == from_outside;

            return pad;
        }

        public void AddValue(LexicalInfo info)
        {
            while (Lexicals.Count <= info.Index)
                Lexicals.Add(null);
            if (Lexicals[info.Index] == null)
                Lexicals[info.Index] = info;
        }

        public P5ScratchPad NewScope(Runtime runtime)
        {
            if (Complete)
                return this;

            P5ScratchPad scope = new P5ScratchPad(this, Lexicals);

            foreach (var lex in Lexicals)
            {
                if (lex == null)
                    continue;

                if (lex.OuterIndex != -1)
                    continue;
                while (scope.Count <= lex.Index)
                    scope.Add(null);
                if (lex.Slot == Opcode.Sigil.SCALAR)
                    scope[lex.Index] = new P5Scalar(runtime);
                else if (lex.Slot == Opcode.Sigil.ARRAY)
                    scope[lex.Index] = new P5Array(runtime);
                else if (lex.Slot == Opcode.Sigil.HASH)
                    scope[lex.Index] = new P5Hash(runtime);
            }

            return scope;
        }

        public P5ScratchPad CloseOver(Runtime runtime, P5ScratchPad outer)
        {
            P5ScratchPad closure = new P5ScratchPad(this, Lexicals);

            foreach (var lex in Lexicals)
            {
                if (!lex.InPad || lex.OuterIndex == -1 || lex.FromMain)
                    continue;
                while (closure.Count <= lex.Index)
                    closure.Add(null);
                closure[lex.Index] = outer[lex.OuterIndex];
            }

            return closure;
        }

        public object GetOrCreateValue(Runtime runtime, Opcode.Sigil slot,
                                       int index)
        {
            if (Count > index && this[index] != null)
                return this[index];

            while (Count <= index)
                Add(null);
            if (slot == Opcode.Sigil.SCALAR)
                this[index] = new P5Scalar(runtime);
            else if (slot == Opcode.Sigil.ARRAY)
                this[index] = new P5Array(runtime);
            else if (slot == Opcode.Sigil.HASH)
                this[index] = new P5Hash(runtime);

            return this[index];
        }

        public object GetScalar(Runtime runtime, int index)
        {
            return this[index] != null ? this[index] : this[index] = new P5Scalar(runtime);
        }

        public object GetArray(Runtime runtime, int index)
        {
            return this[index] != null ? this[index] : this[index] = new P5Array(runtime);
        }

        public object GetHash(Runtime runtime, int index)
        {
            return this[index] != null ? this[index] : this[index] = new P5Hash(runtime);
        }

        private List<LexicalInfo> Lexicals;
        private bool Complete;
    }
}
