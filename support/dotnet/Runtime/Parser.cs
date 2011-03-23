using org.mbarbon.p.values;

namespace org.mbarbon.p.runtime
{
    public class Parser
    {
        public Parser(Runtime runtime)
        {
            parser_runtime = new Runtime();
            parser_runtime.NativeRegex = true;

            // find compiled code
            var parser_assembly = System.Reflection.Assembly.Load("Language.P.Net.Parser");

            parser_runtime.ModuleLoaders.Insert(0, new AssemblyModuleLoader(parser_assembly));

            // load Language::P frontend
            Builtins.RequireFile(parser_runtime,
                                 Opcode.ContextValues.VOID,
                                 new P5Scalar(parser_runtime, "Language/P.pm"));

            // create generator
            generator = new DynamicGenerator(runtime, parser_runtime);

            // instantiate parser
            P5Array arglist_parser =
                new P5Array(parser_runtime,
                            new P5Scalar(parser_runtime, "Language::P::Parser"),
                            GetInit(runtime));
            parser_template = arglist_parser.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "new") as P5Scalar;
        }

        public P5Code ParseFile(Runtime runtime, string program, bool is_main)
        {
            P5Array arglist_safe_instance =
                new P5Array(parser_runtime,
                            parser_template);
            var parser = arglist_safe_instance.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "safe_instance");

            P5Array arglist_parse_file =
                new P5Array(parser_runtime,
                            parser,
                            new P5Scalar(parser_runtime, program),
                            new P5Scalar(parser_runtime, 3));

            IP5Any res;
            try
            {
                res = arglist_parse_file.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "parse_file");
            }
            catch (System.Reflection.TargetInvocationException te)
            {
                var e = te.InnerException as P5Exception;

                if (e == null)
                    throw te;
                else
                    throw FixupException(e);
            }
            catch (P5Exception e)
            {
                throw FixupException(e);
            }

            return NetGlue.UnwrapValue(res, typeof(P5Code)) as P5Code;
        }

        private P5Scalar GetInit(Runtime runtime)
        {
            P5Hash init = new P5Hash(parser_runtime);

            init.SetItem(parser_runtime, "generator",
                         new P5Scalar(new P5NetWrapper(generator)));
            init.SetItem(parser_runtime, "runtime",
                         new P5Scalar(new P5NetWrapper(runtime)));

            return new P5Scalar(parser_runtime, init);
        }

        public void Run(Runtime runtime, string[] args)
        {
            P5Array argv = new P5Array(parser_runtime);

            foreach (var arg in args)
                argv.Push(parser_runtime, new P5Scalar(parser_runtime, arg));

            P5Array arglist_new =
                new P5Array(parser_runtime,
                            new P5Scalar(parser_runtime, "Language::P"),
                            new P5Scalar(parser_runtime, argv),
                            GetInit(runtime));

            try
            {
                var p = arglist_new.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "new_from_argv");

                P5Array arglist_run = new P5Array(parser_runtime, p);
                arglist_run.CallMethod(parser_runtime, Opcode.ContextValues.VOID, "run");
            }
            catch (System.Reflection.TargetInvocationException te)
            {
                var e = te.InnerException as P5Exception;

                if (e == null)
                    throw te;
                else
                    throw FixupException(e);
            }
            catch (P5Exception e)
            {
                throw FixupException(e);
            }
        }

        private P5Exception FixupException(P5Exception e)
        {
            // TODO required until Language::P::Exception can be derived
            //      from P5Exception
            if (e.Reference != null)
            {
                var stash = e.Reference.BlessedReferenceStash(parser_runtime);
                var l_p_e = parser_runtime.SymbolTable.GetPackage(parser_runtime, "Language::P::Exception", false);

                if (stash.IsDerivedFrom(parser_runtime, l_p_e))
                {
                    P5Array arglist_format_message =
                        new P5Array(parser_runtime,
                                    e.Reference);
                    var msg = arglist_format_message.CallMethod(parser_runtime, Opcode.ContextValues.SCALAR, "format_message");

                    return new P5Exception(parser_runtime, msg.AsString(parser_runtime));
                }
            }

            return e;
        }

        private DynamicGenerator generator;
        private Runtime parser_runtime;
        private P5Scalar parser_template;
    }
}
