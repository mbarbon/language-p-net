using org.mbarbon.p.values;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    internal class DynamicModuleGenerator
    {
        internal DynamicModuleGenerator(Runtime _runtime)
        {
            runtime = _runtime;
            subroutines = new Dictionary<Subroutine, P5Code>();
            regexes = new Dictionary<Subroutine, IP5Regex>();
        }

        internal void CreateMainPad(Subroutine sub)
        {
            main_pad = P5ScratchPad.CreateSubPad(sub.Lexicals, null).NewScope(runtime);
        }

        internal void Generate(Subroutine sub)
        {
            if (sub.IsRegex)
                GenerateRegex(sub);
            else
                GenerateSub(sub);
        }

        internal IP5Regex GenerateRegex(Subroutine sub)
        {
            var rx = runtime.NativeRegex ? RegexGenerator.GenerateNetRegex(sub) : RegexGenerator.GenerateRegex(sub);

            regexes[sub] = rx;

            return rx;
        }

        internal P5Code GenerateSub(Subroutine sub)
        {
            if (subroutines.ContainsKey(sub))
                return subroutines[sub];

            if (sub.Inner != null)
                foreach (var inner in sub.Inner)
                    Generate(inner);

            var sg = new DynamicSubGenerator(runtime, this);
            var body = sg.Generate(sub, sub.IsMain);
            var deleg = body.Compile();
            var code = new P5Code(sub.Name, sub.Prototype, deleg, sub.IsMain);

            if (sub.IsMain)
                code.ScratchPad = main_pad;
            else
                code.ScratchPad = P5ScratchPad.CreateSubPad(sub.Lexicals,
                                                            main_pad);

            if (sub.Name != null)
            {
                if (sub.Name == "BEGIN" || sub.Name.EndsWith("::BEGIN"))
                    code.Call(runtime, Opcode.ContextValues.VOID,
                              new P5Array(runtime));
                else
                    runtime.SymbolTable.DefineCode(runtime, sub.Name, code);
            }

            subroutines[sub] = code;

            return code;
        }

        internal P5Code GetSubroutine(Subroutine sub)
        {
            return subroutines[sub];
        }

        internal IP5Regex GetRegex(Subroutine sub)
        {
            return regexes[sub];
        }

        private P5ScratchPad main_pad;
        private Runtime runtime;
        private Dictionary<Subroutine, P5Code> subroutines;
        private Dictionary<Subroutine, IP5Regex> regexes;
    }
}
