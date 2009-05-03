using System.IO;

namespace org.mbarbon.p.runtime
{
    public class Serializer
    {
        public static CompilationUnit ReadCompilationUnit(string file_name)
        {
            BinaryReader reader = new BinaryReader(File.Open(file_name, FileMode.Open));

            int count = reader.ReadInt32();
            var cu = new CompilationUnit(file_name, count);

            for (int i = 0; i < count; ++i)
            {
                cu.Subroutines[i] = ReadSubroutine(reader);
            }

            return cu;
        }

        public static Subroutine ReadSubroutine(BinaryReader reader)
        {
            var name = ReadString(reader);
            int count = reader.ReadInt32();
            var sub = new Subroutine(count);

            sub.Name = name;
            for (int i = 0; i < count; ++i)
            {
                sub.BasicBlocks[i] = ReadBasicBlock(reader);
            }

            return sub;
        }

        public static BasicBlock ReadBasicBlock(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            var bb = new BasicBlock(count);

            for (int i = 0; i < count; ++i)
            {
                bb.Opcodes[i] = ReadOpcode(reader);
            }

            return bb;
        }

        // FIXME needs to be autogenerated
        public static Opcode ReadOpcode(BinaryReader reader)
        {
            var num = (Opcode.OpNumber)reader.ReadInt16();
            Opcode op;

            switch (num)
            {
            case Opcode.OpNumber.OP_CONSTANT_STRING:
            case Opcode.OpNumber.OP_FRESH_STRING:
                ConstantString cs = new ConstantString();
                op = cs;
                cs.Value = ReadString(reader);
                break;
            case Opcode.OpNumber.OP_CONSTANT_INTEGER:
                ConstantInt ci = new ConstantInt();
                op = ci;
                ci.Value = reader.ReadInt32();
                break;
            case Opcode.OpNumber.OP_GLOBAL:
                Global gl = new Global();
                op = gl;
                gl.Name = ReadString(reader);
                gl.Slot = reader.ReadByte();
                break;
            case Opcode.OpNumber.OP_CALL:
                Call ca = new Call();
                op = ca;
                ca.CallContext = reader.ReadByte();
                break;
            case Opcode.OpNumber.OP_GET:
            case Opcode.OpNumber.OP_SET:
                GetSet gs = new GetSet();
                op = gs;
                gs.Variable = reader.ReadInt32();
                break;
            case Opcode.OpNumber.OP_JUMP:
            case Opcode.OpNumber.OP_JUMP_IF_FALSE:
            case Opcode.OpNumber.OP_JUMP_IF_F_EQ:
            case Opcode.OpNumber.OP_JUMP_IF_F_GE:
            case Opcode.OpNumber.OP_JUMP_IF_F_GT:
            case Opcode.OpNumber.OP_JUMP_IF_F_LE:
            case Opcode.OpNumber.OP_JUMP_IF_F_LT:
            case Opcode.OpNumber.OP_JUMP_IF_F_NE:
            case Opcode.OpNumber.OP_JUMP_IF_NULL:
            case Opcode.OpNumber.OP_JUMP_IF_S_EQ:
            case Opcode.OpNumber.OP_JUMP_IF_S_GE:
            case Opcode.OpNumber.OP_JUMP_IF_S_GT:
            case Opcode.OpNumber.OP_JUMP_IF_S_LE:
            case Opcode.OpNumber.OP_JUMP_IF_S_LT:
            case Opcode.OpNumber.OP_JUMP_IF_S_NE:
            case Opcode.OpNumber.OP_JUMP_IF_TRUE:
                Jump jt = new Jump();
                op = jt;
                jt.To = reader.ReadInt32();
                break;
            case Opcode.OpNumber.OP_LEXICAL_PAD:
            case Opcode.OpNumber.OP_LEXICAL:
            case Opcode.OpNumber.OP_LEXICAL_PAD_SET:
            case Opcode.OpNumber.OP_LEXICAL_SET:
            case Opcode.OpNumber.OP_LEXICAL_PAD_CLEAR:
            case Opcode.OpNumber.OP_LEXICAL_CLEAR:
                Lexical lx = new Lexical();
                op = lx;
                lx.Index = reader.ReadInt32();
                break;
            default:
                op = new Opcode();
                break;
            }

            op.Number = num;
            int count = reader.ReadInt32();
            op.Childs = new Opcode[count];
            
            for (int i = 0; i < count; ++i)
            {
                op.Childs[i] = ReadOpcode(reader);
            }

            return op;
        }

        public static string ReadString(BinaryReader reader)
        {
            int size = reader.ReadInt32();
            if (size == 0)
                return "";
            
            byte[] bytes = reader.ReadBytes(size);

            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
    
    public class Opcode
    {
        public enum OpNumber : short
        {
            OP_CONSTANT_STRING = 22,
            OP_GLOBAL         = 70,
            OP_MAKE_LIST      = 106,
            OP_PRINT          = 134,
            OP_POP            = 128,
            OP_END            = 38,
            OP_GET            = 68,
            OP_SET            = 159,
            OP_JUMP           = 77,
            OP_JUMP_IF_FALSE   = 78,
            OP_JUMP_IF_F_EQ    = 79,
            OP_JUMP_IF_F_GE    = 80,
            OP_JUMP_IF_F_GT    = 81,
            OP_JUMP_IF_F_LE    = 82,
            OP_JUMP_IF_F_LT    = 83,
            OP_JUMP_IF_F_NE    = 84,
            OP_JUMP_IF_NULL    = 85,
            OP_JUMP_IF_S_EQ    = 86,
            OP_JUMP_IF_S_GE    = 87,
            OP_JUMP_IF_S_GT    = 88,
            OP_JUMP_IF_S_LE    = 89,
            OP_JUMP_IF_S_LT    = 90,
            OP_JUMP_IF_S_NE    = 91,
            OP_JUMP_IF_TRUE    = 92,
            OP_CONSTANT_INTEGER = 20,
            OP_FRESH_STRING     = 40,
            OP_ASSIGN           = 6,
            OP_CONCAT_ASSIGN    = 18,
            OP_ARRAY_LENGTH     = 5,
            OP_CALL            = 13,
            OP_RETURN          = 147,
            OP_ARRAY_ELEMENT    = 4,
            OP_LOG_NOT         = 102,
            OP_DEFINED         = 25,
            OP_CONSTANT_UNDEF   = 24,
            OP_LEXICAL         = 93,
            OP_LEXICAL_PAD      = 95,
            OP_LEXICAL_SET      = 98,
            OP_LEXICAL_PAD_SET  = 97,
            OP_LEXICAL_CLEAR    = 94,
            OP_LEXICAL_PAD_CLEAR = 96,
            OP_ADD              = 2,
            OP_SUBTRACT         = 168,
            OP_CONCAT          = 17,
            OP_HASH_ELEMENT     = 74,
        }

        public enum Context
        {
            CALLER = 1,
            VOID   = 2,
            SCALAR = 4,
            LIST   = 8,
        }

        public OpNumber Number;
        public Opcode[] Childs;
    }

    public class Global : Opcode
    {
        public string Name;
        public int Slot;
    }

    public class ConstantInt : Opcode
    {
        public int Value;
    }

    public class ConstantString : Opcode
    {
        public string Value;
    }

    public class GetSet : Opcode
    {
        public int Variable;
    }

    public class Jump : Opcode
    {
        public int To;
    }

    public class Call : Opcode
    {
        public int CallContext;
    }

    public class Lexical : Opcode
    {
        public int Index;
    }

    public class BasicBlock
    {
        public BasicBlock(int opCount)
        {
            Opcodes = new Opcode[opCount];
        }
        
        public int Index;
        public Opcode[] Opcodes;
    }

    public class Subroutine
    {
        public Subroutine(int blockCount)
        {
            BasicBlocks = new BasicBlock[blockCount];
        }
        
        public string Name;
        public BasicBlock[] BasicBlocks;
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
}
