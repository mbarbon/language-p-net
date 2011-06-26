using org.mbarbon.p.values;

namespace org.mbarbon.p.runtime
{
    public partial class Runtime
    {
        // required by Language::P
        public void run_file(string program, bool is_main)
        {
            var code = parser.ParseFile(this, program, is_main);

            if (!CompileOnly)
                code.CallMain(this);
        }

        public void run_string(string program, string file, int line)
        {
            var code = parser.ParseString(this, program, file, line);

            if (!CompileOnly)
                code.CallMain(this);
        }

        public IP5Value get_symbol(string name, Opcode.Sigil sigil)
        {
            switch (sigil)
            {
            case Opcode.Sigil.SCALAR:
                return SymbolTable.GetScalar(this, name, false);
            case Opcode.Sigil.ARRAY:
                return SymbolTable.GetArray(this, name, false);
            case Opcode.Sigil.HASH:
                return SymbolTable.GetHash(this, name, false);
            case Opcode.Sigil.SUB:
                return SymbolTable.GetCode(this, name, false);
            case Opcode.Sigil.GLOB:
                return SymbolTable.GetGlob(this, name, false);
            case Opcode.Sigil.HANDLE:
                return SymbolTable.GetHandle(this, name, false);
            default:
                return null;
            }
        }

        public bool has_package(string name)
        {
            return SymbolTable.GetPackage(this, name, false) != null;
        }

        public bool is_declared(string name, Opcode.Sigil sigil)
        {
            var glob = SymbolTable.GetGlob(this, name, false);

            // TODO implement imported check, and setting imported
            //      flag in assignment
            return false;
        }

        public Parser parser
        {
            get
            {
                if (parser_instance == null)
                    parser_instance = new Parser(this);

                return parser_instance;
            }
        }

        private Parser parser_instance;
    }
}

namespace org.mbarbon.p.values
{
    public partial class P5Code
    {
        public int[] prototype() { return proto; }
        public bool is_constant() { return const_flags != 0; }
        public object constant_value() { return const_value; }
        public int constant_flags() { return const_flags; }
    }
}
