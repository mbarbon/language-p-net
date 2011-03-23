using org.mbarbon.p.values;

using System.Reflection.Emit;
using System.Reflection;
using Type = System.Type;
using DebuggableAttribute = System.Diagnostics.DebuggableAttribute;

namespace org.mbarbon.p.runtime
{
    public partial class Generator
    {
        public Generator(Runtime _runtime, string _assembly_name)
        {
            runtime = _runtime;

            // create module builder
            var file_info = new System.IO.FileInfo(_assembly_name);
            var asm_name = new AssemblyName(file_info.Name  + ".dll");
            var asm_builder =
                System.AppDomain.CurrentDomain.DefineDynamicAssembly(
                    asm_name, AssemblyBuilderAccess.RunAndSave, file_info.Directory.FullName);

            // TODO make this an option
            Type daType = typeof(DebuggableAttribute);
            ConstructorInfo daCtor = daType.GetConstructor(
                new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
            CustomAttributeBuilder daBuilder = new CustomAttributeBuilder(
                daCtor, new object[] {
                    DebuggableAttribute.DebuggingModes.DisableOptimizations|
                    DebuggableAttribute.DebuggingModes.Default });
            asm_builder.SetCustomAttribute(daBuilder);

            mod_builder = asm_builder.DefineDynamicModule(
                file_info.Name, file_info.Name + ".dll", true);
        }

        public Type Generate(CompilationUnit cu)
        {
            TypeBuilder perl_module = mod_builder.DefineType(cu.FileName, TypeAttributes.Public);
            var perl_mod_generator = new StaticModuleGenerator(perl_module, runtime.NativeRegex);

            foreach (var sub in cu.Subroutines)
            {
                if (sub.IsRegex)
                    perl_mod_generator.AddRegexInfo(sub);
                else
                    perl_mod_generator.AddSubInfo(sub);
            }

            foreach (var sub in cu.Subroutines)
            {
                if (sub.IsRegex)
                    perl_mod_generator.AddRegex(sub);
                else
                    perl_mod_generator.AddMethod(sub);
            }

            return perl_mod_generator.CompleteGeneration(cu.Subroutines,
                                                         runtime);
        }

        public P5Code GenerateAndLoad(CompilationUnit cu)
        {
            Type mod = Generate(cu);
            object main_sub = mod.GetMethod("InitModule")
                                  .Invoke(null, new object[] { runtime });

            return (P5Code)main_sub;
        }

        public AssemblyBuilder Assembly
        {
            get { return mod_builder.Assembly as AssemblyBuilder; }
        }

        Runtime runtime;
        ModuleBuilder mod_builder;
    }
}
