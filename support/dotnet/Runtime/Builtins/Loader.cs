using org.mbarbon.p.values;
using Path = System.IO.Path;
using File = System.IO.File;

namespace org.mbarbon.p.runtime
{
    public interface IModuleLoader
    {
        IP5Any TryLoad(Runtime runtime, Opcode.ContextValues context, string file);
    }

    internal class IncModuleLoader : IModuleLoader
    {
        public IP5Any TryLoad(Runtime runtime, Opcode.ContextValues context, string file)
        {
            var path = Builtins.SearchFile(runtime, file);
            if (path == null)
                return null;

            P5Code mod;
            var cu = Serializer.ReadCompilationUnit(runtime, path);
            mod = new DynamicGenerator(runtime).GenerateAndLoad(cu);

            var ret = mod.Call(runtime, context, null);

            var inc = runtime.SymbolTable.GetHash(runtime, "INC", true);
            inc.SetItem(runtime, file, new P5Scalar(runtime, path));

            return ret;
        }
    }

    public partial class Builtins
    {
        internal static string SearchFile(Runtime runtime, string file)
        {
            string file_pb;
            if (Path.GetExtension(file) != ".pb")
                file_pb = file + ".pb";
            else
                file_pb = file;

            var inc = runtime.SymbolTable.GetArray(runtime, "INC", true);
            foreach (var i in inc)
            {
                var iStr = i.AsString(runtime);
                var path = Path.Combine(iStr, file);
                var path_pb = Path.Combine(iStr, file_pb);

                if (!runtime.IgnoreBytecode && File.Exists(path_pb))
                    return path_pb;
                // TODO can only load bytecode files for now
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        public static IP5Any LoadFile(Runtime runtime,
                                      Opcode.ContextValues context,
                                      string file)
        {
            IP5Any ret = null;

            foreach (var loader in runtime.ModuleLoaders)
            {
                ret = loader.TryLoad(runtime, context, file);

                if (ret != null)
                    return ret;
            }

            return ret;
        }

        public static IP5Any DoFile(Runtime runtime,
                                    Opcode.ContextValues context,
                                    P5Scalar file)
        {
            var file_s = file.AsString(runtime);

            var ret = LoadFile(runtime, context, file_s);
            if (ret == null)
                return new P5Scalar(runtime);

            return ret;
        }

        public static IP5Any RequireFile(Runtime runtime,
                                         Opcode.ContextValues context,
                                         P5Scalar file)
        {
            if (file.IsInteger(runtime) || file.IsFloat(runtime))
            {
                var value = file.AsFloat(runtime);
                var version = runtime.SymbolTable.GetScalar(runtime, "]", false);
                var version_f = version.AsFloat(runtime);

                if (version_f >= value)
                    return new P5Scalar(runtime, true);

                var msg = string.Format("Perl {0:F} required--this is only {1:F} stopped.", value, version_f);

                throw new P5Exception(runtime, msg);
            }

            var file_s = file.AsString(runtime);
            var inc = runtime.SymbolTable.GetHash(runtime, "INC", true);

            if (inc.ExistsKey(runtime, file_s))
                return new P5Scalar(runtime, 1);

            var ret = LoadFile(runtime, context, file_s);
            if (ret == null)
            {
                var message = new System.Text.StringBuilder();
                var inc_a = runtime.SymbolTable.GetArray(runtime, "INC", true);

                message.Append(string.Format("Can't locate {0:S} in @INC (@INC contains:", file_s));
                foreach (var dir in inc_a)
                {
                    message.Append(" ");
                    message.Append(dir.AsString(runtime));
                }
                message.Append(")");

                throw new P5Exception(runtime, message.ToString());
            }

            return ret;
        }
    }
}
