using org.mbarbon.p.values;

using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    internal class RegexGenerator
    {
        public static IP5Regex GenerateNetRegex(Subroutine sub)
        {
            return new NetRegex(sub.OriginalRegex);
        }

        public static IP5Regex GenerateRegex(Subroutine sub)
        {
            var opmap = new Dictionary<BasicBlock, int>();
            var quantifiers = new List<RxQuantifier>();
            var ops = new List<P5Regex.Op>();
            var targets = new List<int>();
            var exact = new List<string>();
            var classes = new List<RxClass>();
            int captures = 0, saved = 0, count = 0;

            foreach (var bb in sub.BasicBlocks)
                opmap[bb] = count++;

            foreach (var bb in sub.BasicBlocks)
            {
                targets.Add(ops.Count);

                foreach (var op in bb.Opcodes)
                {
                    switch (op.Number)
                    {
                    case Opcode.OpNumber.OP_RX_ANY:
                    case Opcode.OpNumber.OP_RX_ANY_NONEWLINE:
                    case Opcode.OpNumber.OP_RX_FAIL:
                    case Opcode.OpNumber.OP_RX_POP_STATE:
                    case Opcode.OpNumber.OP_RX_BEGINNING:
                    case Opcode.OpNumber.OP_RX_END_OR_NEWLINE:
                    case Opcode.OpNumber.OP_RX_START_MATCH:
                        ops.Add(new P5Regex.Op(op.Number));
                        break;
                    case Opcode.OpNumber.OP_RX_ACCEPT:
                    {
                        var ac = (RegexAccept)op;

                        ops.Add(new P5Regex.Op(ac.Number, ac.Groups));
                        break;
                    }
                    case Opcode.OpNumber.OP_RX_EXACT:
                    case Opcode.OpNumber.OP_RX_EXACT_I:
                    {
                        var ex = (RegexExact)op;

                        ops.Add(new P5Regex.Op(ex.Number, exact.Count));
                        exact.Add(ex.Characters);
                        break;
                    }
                    case Opcode.OpNumber.OP_RX_SAVE_POS:
                    case Opcode.OpNumber.OP_RX_RESTORE_POS:
                    {
                        var st = (RegexState)op;

                        ops.Add(new P5Regex.Op(st.Number, st.Index));
                        ++saved;
                        break;
                    }
                    case Opcode.OpNumber.OP_RX_CLASS:
                    {
                        var cl = (RegexClass)op;
                        string ex = cl.Elements;

                        if ((cl.Flags & 1) != 0)
                            ex = ex.ToLower() + ex.ToUpper();

                        ops.Add(new P5Regex.Op(cl.Number, classes.Count));
                        classes.Add(new RxClass(ex, cl.Flags & ~1));
                        break;
                    }
                    case Opcode.OpNumber.OP_RX_START_GROUP:
                    {
                        var gr = (RegexStartGroup)op;

                        ops.Add(new P5Regex.Op(gr.Number, opmap[gr.To]));
                        break;
                    }
                    case Opcode.OpNumber.OP_RX_TRY:
                    {
                        var tr = (RegexTry)op;

                        ops.Add(new P5Regex.Op(tr.Number, opmap[tr.To]));
                        break;
                    }
                    case Opcode.OpNumber.OP_RX_BACKTRACK:
                    {
                        var tr = (RegexBacktrack)op;

                        ops.Add(new P5Regex.Op(tr.Number, opmap[tr.To]));
                        break;
                    }
                    case Opcode.OpNumber.OP_RX_QUANTIFIER:
                    {
                        var qu = (RegexQuantifier)op;

                        ops.Add(new P5Regex.Op(qu.Number, quantifiers.Count));
                        quantifiers.Add(
                            new RxQuantifier(qu.Min, qu.Max, qu.Greedy != 0,
                                             opmap[qu.To], qu.Group,
                                             qu.SubgroupsStart,
                                             qu.SubgroupsEnd));
                        if (captures <= qu.Group)
                            captures = qu.Group + 1;

                        break;
                    }
                    case Opcode.OpNumber.OP_RX_CAPTURE_START:
                    case Opcode.OpNumber.OP_RX_CAPTURE_END:
                    {
                        var ca = (RegexCapture)op;

                        ops.Add(new P5Regex.Op(ca.Number, ca.Group));
                        if (captures <= ca.Group)
                            captures = ca.Group + 1;

                        break;
                    }
                    case Opcode.OpNumber.OP_JUMP:
                    {
                        var ju = (Jump)op;

                        ops.Add(new P5Regex.Op(ju.Number, opmap[ju.To]));
                        break;
                    }
                    default:
                        throw new System.Exception(string.Format("Unhandled opcode {0:S} in regex generation", op.Number.ToString()));
                    }
                }
            }

            return new P5Regex(ops.ToArray(), targets.ToArray(),
                               exact.ToArray(), quantifiers.ToArray(),
                               classes.ToArray(), captures, saved,
                               sub.OriginalRegex);
        }
    }
}
