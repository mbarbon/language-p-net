using System.IO;
using System.Collections.Generic;
using P5Handle = org.mbarbon.p.values.P5Handle;

namespace org.mbarbon.p.runtime
{
    public partial class Serializer
    {
        public static CompilationUnit ReadCompilationUnit(Runtime runtime,
                                                          string file_name)
        {
            CompilationUnit cu;

            using (var fs = File.Open(file_name, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(fs);
                files = new List<string>();

                int count = reader.ReadInt32();
                int has_data = reader.ReadInt32();
                cu = new CompilationUnit(file_name, count);

                for (int i = 0; i < count; ++i)
                    cu.Subroutines[i] = new Subroutine();

                for (int i = 0; i < count; ++i)
                    ReadSubroutine(reader, cu.Subroutines, cu.Subroutines[i]);

                if (has_data != 0)
                {
                    var name = ReadString(reader);
                    var value = ReadString(reader);
                    var glob = runtime.SymbolTable.GetGlob(runtime, name + "::DATA", true);
                    var input = new System.IO.StringReader(value);

                    glob.Handle = new P5Handle(runtime, input, null);
                }
            }
            files = null;

            return cu;
        }

        public static Subroutine ReadSubroutine(BinaryReader reader, Subroutine[] subs, Subroutine sub)
        {
            var name = ReadStringUndef(reader);
            int proto_count = reader.ReadInt32();
            int[] proto = null;

            if (proto_count >= 0)
            {
                proto = new int[proto_count];
                for (int i = 0; i < proto_count; ++i)
                    proto[i] = reader.ReadInt32();
            }

            int flags = reader.ReadByte();
            int outer_sub = reader.ReadInt32();
            int lex_count = reader.ReadInt32();
            int scope_count = reader.ReadInt32();
            int state_count = reader.ReadInt32();
            int bb_count = reader.ReadInt32();
            string regex = null;

            if (flags == (int)Subroutine.CodeType.REGEX)
                regex = ReadString(reader);

            var lexicals = new List<LexicalInfo>(lex_count);
            for (int i = 0; i < lex_count; ++i)
                lexicals.Add(ReadLexical(reader));

            sub.BasicBlocks = new List<BasicBlock>();
            if (outer_sub >= 0)
            {
                sub.Outer = subs[outer_sub];
                if (subs[outer_sub].Inner == null)
                    subs[outer_sub].Inner = new List<Subroutine>();
                subs[outer_sub].Inner.Add(sub);
            }
            sub.Lexicals = lexicals;
            sub.Name = name;
            sub.Prototype = proto;
            sub.Flags = flags;
            sub.OriginalRegex = regex;

            for (int i = 0; i < bb_count; ++i)
                sub.BasicBlocks.Add(new BasicBlock());

            sub.Scopes = new List<Scope>(scope_count);
            for (int i = 0; i < scope_count; ++i)
                sub.Scopes.Add(ReadScope(reader, subs, sub));

            sub.LexicalStates = new List<LexicalState>(state_count);
            for (int i = 0; i < state_count; ++i)
                sub.LexicalStates.Add(ReadLexicalState(reader));

            for (int i = 0; i < bb_count; ++i)
                // returns null if the block is dead
                sub.BasicBlocks[i] = ReadBasicBlock(reader, i, subs, sub);

            return sub;
        }

        public static LexicalInfo ReadLexical(BinaryReader reader)
        {
            var info = new LexicalInfo();

            info.Level = reader.ReadInt32();
            info.Index = reader.ReadInt32();
            info.OuterIndex = reader.ReadInt32();
            info.Name = ReadString(reader);
            info.Slot = (Opcode.Sigil)reader.ReadByte();
            info.InPad = reader.ReadByte() != 0;
            info.FromMain = reader.ReadByte() != 0;

            return info;
        }

        public static LexicalState ReadLexicalState(BinaryReader reader)
        {
            var state = new LexicalState();

            state.Scope = reader.ReadInt32();
            state.Hints = reader.ReadInt32();
            state.Package = ReadString(reader);
            state.Warnings = ReadString(reader);

            return state;
        }

        public static Scope ReadScope(BinaryReader reader, Subroutine[] subs,
                                      Subroutine sub)
        {
            var scope = new Scope();

            scope.Outer = reader.ReadInt32();
            scope.Id = reader.ReadInt32();
            scope.Flags = reader.ReadInt32();
            scope.Context = reader.ReadInt32();
            ReadPos(reader, out scope.Start);
            ReadPos(reader, out scope.End);
            scope.LexicalState = reader.ReadInt32();
            int exc_idx = reader.ReadInt32();
            if (exc_idx >= 0)
                scope.Exception = sub.BasicBlocks[exc_idx];

            int leave_count = reader.ReadInt32();

            scope.Opcodes = new List<List<Opcode>>(leave_count);

            for (int i = 0; i < leave_count; ++i)
            {
                int op_count = reader.ReadInt32();
                scope.Opcodes.Add(new List<Opcode>(op_count));

                for (int j = 0; j < op_count; ++j)
                    scope.Opcodes[i].Add(ReadOpcode(reader, subs, sub));
            }

            return scope;
        }

        public static BasicBlock ReadBasicBlock(BinaryReader reader,
                                                int index, Subroutine[] subs,
                                                Subroutine sub)
        {
            int scope = reader.ReadInt32();
            int count = reader.ReadInt32();
            var bb = sub.BasicBlocks[index];

            if (count == 0)
            {
                bb.Opcodes = null;
                bb.Dead = 2; // TODO enumeration

                return null;
            }

            bb.Scope = scope;

            for (int i = 0; i < count; ++i)
                bb.Opcodes.Add(ReadOpcode(reader, subs, sub));

            return bb;
        }

        // ReadOpcode() is autogenerated; see inc/Opcodes.pm

        public static string ReadString(BinaryReader reader)
        {
            int size = reader.ReadInt32();
            if (size == 0)
                return "";

            byte[] bytes = reader.ReadBytes(size);

            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static string ReadStringUndef(BinaryReader reader)
        {
            uint size = reader.ReadUInt32();
            if (size == 0xffffffff)
                return null;
            if (size == 0)
                return "";

            byte[] bytes = reader.ReadBytes((int)size);

            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static void ReadPos(BinaryReader reader, out Position pos)
        {
            ReadPos(reader, out pos.File, out pos.Line);
        }

        public static void ReadPos(BinaryReader reader,
                                   out string file, out int line)
        {
            var idx = reader.ReadInt16();

            if (idx == -2)
            {
                file = null;
                line = 0;
            }
            else if (idx == -1)
            {
                file = ReadString(reader);
                line = reader.ReadInt32();
                files.Add(file);
            }
            else
            {
                file = files[idx];
                line = reader.ReadInt32();
            }
        }

        private static List<string> files;
    }

    // class Opcode is partially autogenerated; see inc/OpcodesDotNet.pm
    public partial class Opcode
    {
        public enum ContextValues
        {
            CALLER     = 1,
            VOID       = 2,
            SCALAR     = 4,
            LIST       = 8,
            LVALUE     = 16,
            VIVIFY     = 32,
            NOCREATE   = 64,
            MAYBE_LVALUE = 128,
        }

        public const int RX_CASE_INSENSITIVE = 4;
        public const int RX_ONCE             = 16;
        public const int RX_GLOBAL           = 32;
        public const int RX_KEEP             = 64;

        public const int RX_CLASS_WORDS       = 1 << 1;
        public const int RX_CLASS_NOT_WORDS   = 1 << 2;
        public const int RX_CLASS_SPACES      = 1 << 3;
        public const int RX_CLASS_NOT_SPACES  = 1 << 4;
        public const int RX_CLASS_DIGITS      = 1 << 5;
        public const int RX_CLASS_NOT_DIGITS  = 1 << 6;
        public const int RX_POSIX_ALPHA       = 1 << 10;
        public const int RX_POSIX_ALNUM       = 1 << 11;
        public const int RX_POSIX_ASCII       = 1 << 12;
        public const int RX_POSIX_BLANK       = 1 << 13;
        public const int RX_POSIX_CNTRL       = 1 << 14;
        public const int RX_POSIX_DIGIT       = 1 << 15;
        public const int RX_POSIX_GRAPH       = 1 << 16;
        public const int RX_POSIX_LOWER       = 1 << 17;
        public const int RX_POSIX_PRINT       = 1 << 18;
        public const int RX_POSIX_PUNCT       = 1 << 19;
        public const int RX_POSIX_SPACE       = 1 << 20;
        public const int RX_POSIX_UPPER       = 1 << 21;
        public const int RX_POSIX_WORD        = 1 << 22;
        public const int RX_POSIX_XDIGIT      = 1 << 23;

        public const int FLAG_RX_COMPLEMENT   = 1;
        public const int FLAG_RX_DELETE       = 2;
        public const int FLAG_RX_SQUEEZE      = 4;

        public const int CHANGED_HINTS        = 1;
        public const int CHANGED_WARNINGS     = 2;
        public const int CHANGED_PACKAGE      = 4;
        public const int CHANGED_ALL          = 7;

        public enum Sigil
        {
            SCALAR    = 1,
            ARRAY     = 2,
            HASH      = 3,
            SUB       = 4,
            GLOB      = 5,
            HANDLE    = 7,
            ITERATOR  = 9,
            STASH     = 10,
            INDEXABLE = 11,
        }

        public OpNumber Number;
        public Position Position;
        public int Context;
        public Opcode[] Childs;
    }

    public struct Position
    {
        public string File;
        public int Line;
    }

    // TODO autogenerate all opcode subclasses
    public partial class ListAssign : Opcode
    {
        public byte Common;
    }

    public partial class Global : Opcode
    {
        public string Name;
        public Opcode.Sigil Slot;
    }

    public partial class LocalGlobSlot : Opcode
    {
        public string Name;
        public Opcode.Sigil Slot;
        public int Index;
    }

    public partial class GlobSlot : Opcode
    {
        public Opcode.Sigil Slot;
    }

    public partial class ConstantInt : Opcode
    {
        public int Value;
    }

    public partial class ConstantString : Opcode
    {
        public string Value;
    }

    public partial class ConstantSub : Opcode
    {
        public Subroutine Value;
    }

    public partial class ConstantFloat : Opcode
    {
        public double Value;
    }

    public partial class GetSet : Opcode
    {
        public int Index;
        public Opcode.Sigil Slot;
    }

    public partial class Phi : Opcode
    {
        public Opcode.Sigil[] Slots;
        public int[] Indices;
        public BasicBlock[] Blocks;
    }

    public partial class Jump : Opcode
    {
        public BasicBlock To;
    }

    public partial class CondJump : Opcode
    {
        public BasicBlock To, True, False;
    }

    public partial class LexState : Opcode
    {
        public int Index;
    }

    public partial class Temporary : Opcode
    {
        public int Index;
        public Opcode.Sigil Slot;
    }

    public partial class ElementAccess : Opcode
    {
        public int Create;
    }

    public partial class Lexical : Opcode
    {
        public LexicalInfo LexicalInfo;

        public int LexicalIndex
        {
            get { return LexicalInfo.Index; }
        }

        public Opcode.Sigil Slot
        {
            get { return LexicalInfo.Slot; }
        }
    }

    public partial class LocalLexical : Lexical
    {
        public int Index;
    }

    public partial class LocalElement : Opcode
    {
        public int Index;
    }

    public partial class CallMethod : Opcode
    {
        public string Method;
    }

    public partial class RegexExact : Opcode
    {
        public string Characters;
        public int Length;
    }

    public partial class RegexClass : Opcode
    {
        public string Elements, Ranges;
        public int Flags;
    }

    public partial class RegexAccept : Opcode
    {
        public int Groups;
    }

    public partial class RegexStartGroup : Opcode
    {
        public BasicBlock To;
    }

    public partial class RegexTry : Opcode
    {
        public BasicBlock To;
    }

    public partial class RegexBacktrack : Opcode
    {
        public BasicBlock To;
    }

    public partial class RegexQuantifier : Opcode
    {
        public int Min, Max;
        public byte Greedy;
        public int Group;
        public BasicBlock To, True, False;
        public int SubgroupsStart, SubgroupsEnd;
    }

    public partial class RegexCapture : Opcode
    {
        public int Group;
    }

    public partial class RegexState : Opcode
    {
        public int Index;
    }

    public partial class RegexMatch : Opcode
    {
        public int Index;
        public int Flags;
    }

    public partial class RegexReplace : RegexMatch
    {
        public BasicBlock To;
    }

    public partial class RegexEval : Opcode
    {
        public int Flags;
    }

    public partial class RegexTransliterate : Opcode
    {
        public string Match;
        public string Replacement;
        public int Flags;
    }

    public partial class Scope
    {
        public const int SCOPE_SUB       = 1;
        public const int SCOPE_EVAL      = 2;
        public const int SCOPE_MAIN      = 4;
        public const int SCOPE_LEX_STATE = 8;
        public const int SCOPE_REGEX     = 16;
        public const int SCOPE_VALUE     = 32;

        public Scope()
        {
        }

        public int Outer;
        public int Id;
        public int Flags;
        public int Context;
        public List<List<Opcode>> Opcodes;
        public Position Start;
        public Position End;
        public int LexicalState;
        public BasicBlock Exception;
    }

    public partial class LexicalState
    {
        public LexicalState() { }

        public int Scope;
        public int Hints;
        public string Package;
        public string Warnings;
    }

    public partial class BasicBlock
    {
        public BasicBlock()
        {
            Opcodes = new List<Opcode>();
            Predecessors = new List<BasicBlock>();
            Successors = new List<BasicBlock>();
        }

        public string StartLabel;
        public int Index;
        public int Scope;
        public int Dead;
        public List<Opcode> Opcodes;
        public List<BasicBlock> Predecessors, Successors;
    }

    public partial class Subroutine
    {
        public enum CodeType
        {
            MAIN     = 1,
            SUB      = 2,
            REGEX    = 4,
            EVAL     = 8,
            CONSTANT = 16,
            CONSTANT_PROTOTYPE = 16|32,
        }

        public Subroutine()
        {
        }

        public bool IsMain
        {
            get { return Flags == (int)CodeType.MAIN; }
        }

        public bool IsRegex
        {
            get { return Flags == (int)CodeType.REGEX; }
        }

        public bool IsConstant
        {
            get { return (Flags & (int)CodeType.CONSTANT) != 0; }
        }

        public bool IsConstantPrototype
        {
            get { return (Flags & (int)CodeType.CONSTANT_PROTOTYPE) != 0; }
        }

        public int Flags;
        public int[] Prototype;
        public Subroutine Outer;
        public string Name;
        public List<BasicBlock> BasicBlocks;
        public List<LexicalInfo> Lexicals;
        public List<Scope> Scopes;
        public List<LexicalState> LexicalStates;
        public List<Subroutine> Inner;
        public string OriginalRegex;
    }

    public class CompilationUnit
    {
        public CompilationUnit(string file_name, int subCount)
        {
            Subroutines = new Subroutine[subCount];
            FileName = file_name;
        }

        public string FileName;
        public Subroutine[] Subroutines;
    }

    public partial class LexicalInfo
    {
        public LexicalInfo()
            : this(null, 0, -1, -1, -1, false, false)
        { }

        public LexicalInfo(string name, Opcode.Sigil slot,
                           int level, int index, int outer,
                           bool in_pad, bool from_main)
        {
            Level = level;
            Index = index;
            OuterIndex = outer;
            Slot = slot;
            InPad = in_pad;
            FromMain = from_main;
            Name = name;
        }

        public int Level, Index, OuterIndex;
        public Opcode.Sigil Slot;
        public bool InPad, FromMain;
        public string Name;
    }
}
