using org.mbarbon.p.values;

namespace org.mbarbon.p.runtime
{
    public partial class DynamicGenerator
    {
        public DynamicGenerator(Runtime _runtime)
        {
            runtime = _runtime;
            mod_generator = new DynamicModuleGenerator(runtime);
        }

        public P5Code GenerateAndLoad(CompilationUnit cu)
        {
            P5Code main = null;

            foreach (var sub in cu.Subroutines)
            {
                if (sub.IsRegex)
                    mod_generator.GenerateRegex(sub);
                else
                {
                    P5Code code = mod_generator.GenerateSub(sub);

                    if (sub.IsMain)
                        main = code;
                }
            }

            return main;
        }

        private Runtime runtime;
        private DynamicModuleGenerator mod_generator;
    }
}