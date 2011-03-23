using org.mbarbon.p.runtime;
using org.mbarbon.p.values;

using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace org.mbarbon.p
{
    class MainClass
    {
        public static void ParseCommandLine(Runtime runtime, string[] args,
                                            out string[] argv)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];

                switch (arg)
                {
                case "-Znative-regex":
                    runtime.NativeRegex = true;
                    break;
                case "-Zignore-bytecode":
                    runtime.IgnoreBytecode = true;
                    break;
                default:
                    argv = new string[args.Length - i];
                    for (int j = i; j < args.Length; ++j)
                        argv[j - i] = args[j];

                    return;
                }
            }

            argv = null;

            return;
        }

        public static void Main(string[] args)
        {
            // use the invariant locale as the default
            System.Threading.Thread.CurrentThread.CurrentCulture =
               System.Globalization.CultureInfo.InvariantCulture;

            var runtime = new Runtime();
            string[] argv;

            ParseCommandLine(runtime, args, out argv);

            try
            {
                if (argv[0].EndsWith(".pb"))
                {
                    var cu = Serializer.ReadCompilationUnit(runtime, argv[0]);
                    P5Code main = new DynamicGenerator(runtime).GenerateAndLoad(cu);

                    main.CallMain(runtime);
                }
                else
                {
                    var parser = runtime.parser;

                    parser.Run(runtime, argv);
                }
            }
            catch (System.Reflection.TargetInvocationException te)
            {
                var e = te.InnerException as P5Exception;

                if (e == null)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine(te.InnerException.ToString());
                }
                else
                    System.Console.WriteLine(e.AsString(runtime));
            }
            catch (P5Exception e)
            {
                System.Console.WriteLine(e.AsString(runtime));
            }
        }
    }
}