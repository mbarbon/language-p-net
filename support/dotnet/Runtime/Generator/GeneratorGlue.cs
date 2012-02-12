using org.mbarbon.p.values;
using System.Collections.Generic;
using NetGlue = org.mbarbon.p.runtime.NetGlue;

namespace org.mbarbon.p.runtime
{
    public partial class DynamicGenerator
    {
        public DynamicGenerator(Runtime _runtime, Runtime _parser_runtime) :
            this(_runtime)
        {
            parser_runtime = _parser_runtime;

            // load Language::P::Intermediate frontend
            Builtins.RequireFile(parser_runtime,
                                 Opcode.ContextValues.VOID,
                                 new P5Scalar(parser_runtime, "Language/P/Intermediate/Generator.pm"));

            // set the opcode factory
            var assembly_i = parser_runtime.SymbolTable.GetGlob(parser_runtime, "Language::P::Assembly::i", true);
//             assembly_i.Code = new P5NativeCode("Language::P::Assembly::i",
//                                                new P5Code.Sub(Opcode.WrapCreate));

            // wrap Language::P::Intermediate support classes
            NetGlue.Extend(parser_runtime, "Language::P::Intermediate::Code",
                           "org.mbarbon.p.runtime.Subroutine",
                           "new", false);
            NetGlue.Extend(parser_runtime, "Language::P::Intermediate::BasicBlock",
                           "org.mbarbon.p.runtime.BasicBlock",
                           "new_from_label", false);
            NetGlue.Extend(parser_runtime, "Language::P::Intermediate::Scope",
                           "org.mbarbon.p.runtime.Scope",
                           "new", false);
            NetGlue.Extend(parser_runtime, "Language::P::Intermediate::LexicalState",
                           "org.mbarbon.p.runtime.LexicalState",
                           "new", false);
            NetGlue.Extend(parser_runtime, "Language::P::Intermediate::LexicalInfo",
                           "org.mbarbon.p.runtime.LexicalInfo",
                           "new", false);

            intermediate = CreateIRGenerator();

            // load Language::P::Transform
            Builtins.RequireFile(parser_runtime,
                                 Opcode.ContextValues.VOID,
                                 new P5Scalar(parser_runtime, "Language/P/Intermediate/Transform.pm"));

            transform = CreateTransform();
        }

        public DynamicGenerator(Runtime _runtime, Runtime _parser_runtime,
                                P5Scalar _intermediate, P5Scalar _transform) :
            this(_runtime)
        {
            parser_runtime = _parser_runtime;
            intermediate = _intermediate;
            transform = _transform;
        }

        private P5Scalar CreateIRGenerator()
        {
            P5Array arglist_parser =
                new P5Array(parser_runtime,
                            new P5Scalar(parser_runtime, "Language::P::Intermediate::Generator"),
                            new P5Scalar(parser_runtime, new P5Hash(parser_runtime)));

            return arglist_parser.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "new") as P5Scalar;
        }

        private P5Scalar CreateTransform()
        {
            P5Array arglist_transform =
                new P5Array(parser_runtime,
                            new P5Scalar(parser_runtime, "Language::P::Intermediate::Transform"));

            return arglist_transform.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "new") as P5Scalar;
        }

        private P5Scalar _to_ir(string method, P5Scalar trees)
        {
            P5Array arglist_generate =
                new P5Array(parser_runtime,
                            intermediate,
                            trees);
            var segments = arglist_generate.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, method) as P5Scalar;

            P5Array arglist_transform =
                new P5Array(parser_runtime,
                            transform,
                            segments);
            arglist_transform.CallMethod(parser_runtime, Opcode.ContextValues.VOID, "all_to_tree");

            return segments;
        }

        public bool is_generating()
        {
            return pending != null;
        }

        public DynamicGenerator safe_instance()
        {
            return new DynamicGenerator(runtime, parser_runtime,
                                        CreateIRGenerator(),
                                        CreateTransform());
        }

        // TODO change it to P5Hash when auto-dereference is implemented,
        //      and maybe use a plain .Net map
        public void start_code_generation(P5Scalar args)
        {
            var argmap = args.DereferenceHash(parser_runtime);

            file_name = argmap.GetItem(parser_runtime, "file_name").AsString(parser_runtime);

            pending = new List<P5Scalar>();

            P5Array arglist_create_main =
                new P5Array(parser_runtime,
                            intermediate,
                            new P5Scalar(parser_runtime),
                            new P5Scalar(parser_runtime, false));
            arglist_create_main.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "create_main");
        }

        public void process(P5Scalar tree)
        {
            var use_stash = parser_runtime.SymbolTable.GetPackage(parser_runtime, "Language::P::ParseTree::Use", false);
            var sub_stash = parser_runtime.SymbolTable.GetPackage(parser_runtime, "Language::P::ParseTree::NamedSubroutine", false);
            var lex_stash = parser_runtime.SymbolTable.GetPackage(parser_runtime, "Language::P::ParseTree::LexicalState", false);

            if (tree.BlessedReferenceStash(parser_runtime).IsDerivedFrom(parser_runtime, use_stash))
            {
                var code = _to_ir("generate_use", tree);
                var subs = NetGlue.UnwrapArray<Subroutine>(parser_runtime, code);

                foreach (var sub in subs)
                    mod_generator.Generate(sub);

                return;
            }

            if (tree.BlessedReferenceStash(parser_runtime).IsDerivedFrom(parser_runtime, sub_stash))
            {
                var code = _to_ir("generate_subroutine", tree);
                var subs = NetGlue.UnwrapArray<Subroutine>(parser_runtime, code);

                foreach (var sub in subs)
                    mod_generator.Generate(sub);

                return;
            }

            if (tree.BlessedReferenceStash(parser_runtime).IsDerivedFrom(parser_runtime, lex_stash))
            {
                P5Array arglist_changed =
                    new P5Array(parser_runtime, tree);
                var changed_o = arglist_changed.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "changed");
                int changed = Builtins.ConvertToInteger(parser_runtime, changed_o);

                if ((changed & Opcode.CHANGED_PACKAGE) != 0)
                {
                    P5Array arglist_package =
                        new P5Array(parser_runtime, tree);
                    var pack_o = arglist_package.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "package");
                    string pack = Builtins.ConvertToString(parser_runtime, pack_o);

                    runtime.SymbolTable.GetPackage(runtime, pack, true);
                }
            }

            pending.Add(tree.Clone(parser_runtime, 0) as P5Scalar);
        }

        public P5Code end_code_generation()
        {
            var main_int = _to_ir("generate_bytecode",
                                  new P5Scalar(new P5NetWrapper(pending)));
            var subs = main_int.DereferenceArray(parser_runtime);
            var sub_count = subs.GetCount(parser_runtime);
            var cu = new CompilationUnit(file_name, sub_count);

            for (int i = 0; i < sub_count; ++i)
                cu.Subroutines[i] = (Subroutine)NetGlue.UnwrapValue(subs.GetItem(parser_runtime, i), typeof(Subroutine));

            pending = null;

            return GenerateAndLoad(cu);
        }

        public void add_declaration(string name, int[] prototype)
        {
            var sub = runtime.SymbolTable.GetCode(runtime, name, false);

            if (sub != null && sub.IsDefined(runtime))
                ; // TODO warn about prototype mismatch
            else if (sub != null)
                ; // TODO warn about prototype mismatch
            else
                runtime.SymbolTable.SetCode(runtime, name,
                                            new P5Code(name, prototype));
        }

        public void set_data_handle(string pack, P5Scalar handle)
        {
            var p5pack = runtime.SymbolTable.GetPackage(runtime, pack, false);
            var data = p5pack.GetGlob(runtime, "DATA", true);

            data.Handle = handle.DereferenceHandle(runtime);
        }

        List<P5Scalar> pending;
        string file_name;
        P5Scalar intermediate, transform;
        Runtime parser_runtime;
    }
}
