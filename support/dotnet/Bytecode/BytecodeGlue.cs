using org.mbarbon.p.values;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Subroutine
    {
        public Subroutine(Runtime runtime, P5Scalar args)
        {
            var arg = args.DereferenceHash(runtime);

            Type = arg.GetItem(runtime, "type").AsInteger(runtime);
            Outer = (Subroutine)NetGlue.UnwrapValue(arg.GetItem(runtime, "outer"), typeof(Subroutine));

            var name = arg.GetItem(runtime, "name") as P5Scalar;
            Name = name.IsDefined(runtime) ? name.AsString(runtime) : null;
            if (arg.ExistsKey(runtime, "regex_string"))
                OriginalRegex = arg.GetItem(runtime, "regex_string").AsString(runtime);
            Inner = new List<Subroutine>();
            BasicBlocks = new List<BasicBlock>();
            Lexicals = new List<LexicalInfo>();
            Scopes = new List<Scope>();
            LexicalStates = new List<LexicalState>();

            if (arg.ExistsKey(runtime, "prototype"))
            {
                var proto = arg.GetItem(runtime, "prototype") as P5Scalar;
                if (proto.IsDefined(runtime))
                    Prototype = NetGlue.UnwrapArray<int>(runtime, proto);
            }

            var ls = new LexicalState();

            ls.Scope = 0;
            ls.Hints = 0;
            ls.Package = "main";
            ls.Warnings = null;

            LexicalStates.Add(ls);
        }

        public List<BasicBlock> basic_blocks() { return BasicBlocks; }
        public List<Scope> scopes() { return Scopes; }
        public Subroutine outer() { return Outer; }
        public List<Subroutine> inner() { return Inner; }
        public List<LexicalState> lexical_states() { return LexicalStates; }
        public List<LexicalInfo> lexicals() { return Lexicals; }

        public bool is_main()  { return    Type == (int)CodeType.MAIN
                                        || Type == (int)CodeType.EVAL; }
        public bool is_sub()   { return (Type & (int)CodeType.SUB) != 0; }
        public bool is_regex() { return Type == (int)CodeType.REGEX; }
        public bool is_eval()  { return Type == (int)CodeType.EVAL; }
        public bool is_constant() { return (Type & (int)CodeType.CONSTANT) != 0; }

        public void find_alive_blocks()
        {
            var queue = new List<BasicBlock>();

            queue.Add(BasicBlocks[0]);

            // TODO add exception landing blocks
            foreach (var block in queue)
                block.Dead = 0;

            while (queue.Count > 0)
            {
                var block = queue[0];

                queue.RemoveAt(0);

                foreach (var successor in block.Successors)
                {
                    if (successor.Dead == 0)
                        continue;
                    successor.Dead = 0;
                    queue.Add(successor);
                }
            }
        }
    }

    public partial class BasicBlock
    {
        public BasicBlock(string label, int scope, int dead)
        {
            StartLabel = label;
            Scope = scope;
            Dead = dead;
            Opcodes = new List<Opcode>();
            Predecessors = new List<BasicBlock>();
            Successors = new List<BasicBlock>();
        }

        public int dead() { return Dead; }
        public string start_label() { return StartLabel; }
        public List<Opcode> bytecode() { return Opcodes; }
        public int scope() { return Scope; }
        public void set_scope(int scope) { Scope = scope; }
        public List<BasicBlock> predecessors() { return Predecessors; }
        public List<BasicBlock> successors() { return Successors; }

        // TODO generate from Perl code
        public void add_jump(Opcode op, BasicBlock first)
        {
            if (Opcodes.Count == 0 && Predecessors.Count > 0 && first != this)
            {
                while (first.Successors.Count > 0 && first.Opcodes.Count == 0)
                    first = first.Successors[0];

                var pred = new List<BasicBlock>(Predecessors);
                foreach (var p in pred)
                    p._change_successor(this, first);

                add_successor(first);
            }
            else
                add_jump_unoptimized(op, first);
        }

        public void add_jump(Opcode op, BasicBlock first, BasicBlock second)
        {
            add_jump_unoptimized(op, first, second);
        }

        public void add_jump_unoptimized(Opcode op, BasicBlock first)
        {
            Opcodes.Add(op);
            if (Dead == 2)
                return;
            do_add_jump(first);
        }

        public void add_jump_unoptimized(Opcode op, BasicBlock first,
                                         BasicBlock second)
        {
            Opcodes.Add(op);
            if (Dead == 2)
                return;
            do_add_jump(first);
            do_add_jump(second);
        }

        private void do_add_jump(BasicBlock block)
        {
            add_successor(block);
            block.add_predecessor(this);
            if (block.Opcodes.Count == 0 && block.Successors.Count != 0)
                _change_successor(block, block.Successors[0]);
        }

        public void add_predecessor(BasicBlock block)
        {
            if (Predecessors.Contains(block))
                return;

            Predecessors.Add(block);
        }

        public void add_successor(BasicBlock block)
        {
            if (Successors.Contains(block))
                return;

            Successors.Add(block);
        }

        // TODO rename or remove access
        public void _change_successor(BasicBlock from, BasicBlock to)
        {
            for (int i = 0; i < Successors.Count; ++i)
            {
                if (Successors[i] == from)
                {
                    Successors[i] = to;
                    break;
                }
            }

            var jump = Opcodes[Opcodes.Count - 1] as Jump;
            var cond_jump = Opcodes[Opcodes.Count - 1] as CondJump;
            if (jump != null && jump.To == from)
                jump.To = to;
            else if (cond_jump.True == from)
                cond_jump.True = to;
            else if (cond_jump.False == from)
                cond_jump.False = to;
            // TODO else die

            to.add_predecessor(this);
            for (int i = 0; i < from.Predecessors.Count; ++i)
            {
                if (from.Predecessors[i] == this)
                {
                    from.Predecessors.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public partial class Scope
    {
        public Scope(Runtime runtime, P5Scalar args)
        {
            var arg = args.DereferenceHash(runtime);

            Opcodes = new List<List<Opcode>>();

            Outer = arg.GetItem(runtime, "outer").AsInteger(runtime);
            Id = arg.GetItem(runtime, "id").AsInteger(runtime);
            Flags = arg.GetItem(runtime, "flags").AsInteger(runtime);
            Context = arg.GetItem(runtime, "context").AsInteger(runtime);
            // TODO reference to basic block
            Exception = (BasicBlock)NetGlue.UnwrapValue(arg.GetItem(runtime, "exception"), typeof(BasicBlock));
            LexicalState = arg.GetItem(runtime, "lexical_state").AsInteger(runtime);

            SetPos(runtime, arg.GetItem(runtime, "pos_s"), out Start);
            SetPos(runtime, arg.GetItem(runtime, "pos_e"), out End);
        }

        private void SetPos(Runtime runtime, IP5Any val, out Position pos)
        {
            var scalar = val as P5Scalar;

            if (!scalar.IsDefined(runtime))
            {
                pos.File = null;
                pos.Line = 0;
            }
            else
            {
                var arr = scalar.DereferenceArray(runtime);

                pos.File = arr.GetItem(runtime, 0).AsString(runtime);
                pos.Line = arr.GetItem(runtime, 1).AsInteger(runtime);
            }
        }

        public int id() { return Id; }
        public int lexical_state() { return LexicalState; }
        public List<List<Opcode>> bytecode() { return Opcodes; }
        public int flags() { return Flags; }
        public int outer() { return Outer; }

        public void set_flags(int flags) { Flags = flags; }
        public void set_exception(BasicBlock exc) { Exception = exc; }
    }

    public partial class LexicalState
    {
        public LexicalState(Runtime runtime, P5Scalar args)
        {
            var arg = args.DereferenceHash(runtime);

            Scope = arg.GetItem(runtime, "scope").AsInteger(runtime);
            Hints = arg.GetItem(runtime, "hints").AsInteger(runtime);
            Package = arg.GetItem(runtime, "package").AsString(runtime);
            Warnings = arg.GetItem(runtime, "warnings").AsString(runtime);
        }
    }

    public partial class LexicalInfo
    {
        public LexicalInfo(Runtime runtime, P5Scalar args)
        {
            var arg = args.DereferenceHash(runtime);

            Index = arg.GetItem(runtime, "index").AsInteger(runtime);
            OuterIndex = arg.GetItem(runtime, "outer_index").AsInteger(runtime);
            Name = arg.GetItem(runtime, "name").AsString(runtime);
            Slot = (Opcode.Sigil)arg.GetItem(runtime, "sigil").AsInteger(runtime);
            Level = arg.GetItem(runtime, "level").AsInteger(runtime);
            InPad = arg.GetItem(runtime, "in_pad").AsBoolean(runtime);
        }

        public int index() { return Index; }
        public Opcode.Sigil sigil() { return Slot; }
        public bool in_pad() { return InPad; }
        public int level() { return Level; }

        public void set_index(int index) { Index = index; }
        public void set_declaration(bool unused) { /* only for Toy runtime */ }
        public void set_outer_index(int index) { OuterIndex = index; }
        public void set_from_main(bool from_main) { FromMain = from_main; }
    }

    public partial class Opcode
    {
        public OpNumber opcode_n() { return Number; }
        public Position pos() { return Position; }
        public Opcode[] parameters() { return Childs; }
        public virtual bool is_jump() { return false; }
    }

    public partial class Lexical
    {
        public int lex_index() { return LexicalInfo.Index; }
    }

    public partial class Jump
    {
        public override bool is_jump() { return true; }
    }

    public partial class CondJump
    {
        public BasicBlock to_true() { return True; }
        public BasicBlock to_false() { return False; }
        public override bool is_jump() { return true; }
    }

    public partial class RegexQuantifier
    {
        public BasicBlock to_true() { return True; }
        public BasicBlock to_false() { return False; }
    }
}
