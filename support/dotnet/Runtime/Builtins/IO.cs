using org.mbarbon.p.values;
using System.IO;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        public static object Print(Runtime runtime, object handle, List<object> args)
        {
            var p5handle = handle as P5Handle;
            var writer = handle as TextWriter;

            if (p5handle != null)
                writer = p5handle.Output;

            return Print(runtime, writer, args);
        }

        private static int Print(Runtime runtime, TextWriter handle,
                                 List<object> args)
        {
            foreach (var item in args)
            {
                var p5any = item as IP5Any;

                if (p5any != null)
                    handle.Write(p5any.AsString(runtime));
                else if (item is bool)
                    handle.Write((bool) item ? "1" : "");
                else
                    handle.Write(item.ToString());
            }

            return 1;
        }

        public static IP5Any Readline(Runtime runtime, P5Handle handle,
                                      Opcode.ContextValues cxt)
        {
            if (cxt == Opcode.ContextValues.LIST)
            {
                P5Scalar line;
                var lines = new List<IP5Any>();

                while (handle.Readline(runtime, out line))
                    lines.Add(line);

                return new P5List(runtime, lines);
            }
            else
            {
                P5Scalar line;
                handle.Readline(runtime, out line);

                return line;
            }
        }

        public static P5Scalar Open(Runtime runtime, P5Array args)
        {
            if (args.GetCount(runtime) == 3)
                return Open3Args(runtime,
                                 args.GetItem(runtime, 0) as P5Scalar,
                                 args.GetItem(runtime, 1).AsString(runtime),
                                 args.GetItem(runtime, 2) as P5Scalar);

            throw new System.Exception("Unhandled arg count in open");
        }

        public static P5Scalar Open3Args(Runtime runtime, P5Scalar target, string open_mode, P5Scalar value)
        {
            FileMode mode;
            FileAccess access;
            bool is_read = false, is_write = false, truncate = false;

            switch (open_mode)
            {
            case ">":
                mode = FileMode.OpenOrCreate;
                access = FileAccess.Write;
                is_write = truncate = true;
                break;
            case "<":
                mode = FileMode.Open;
                access = FileAccess.Read;
                is_read = true;
                break;
            case ">>":
                mode = FileMode.Append;
                access = FileAccess.Write;
                is_write = true;
                break;
            case "+>":
                mode = FileMode.OpenOrCreate;
                access = FileAccess.ReadWrite;
                is_read = is_write = truncate = true;
                break;
            case "+<":
                mode = FileMode.OpenOrCreate;
                access = FileAccess.ReadWrite;
                is_read = is_write = true;
                break;
            default:
                throw new P5Exception(runtime, string.Format("Unknown open() mode '{0}'", open_mode));
            }

            FileStream filestream;

            try
            {
                filestream = new FileStream(value.AsString(runtime), mode, access);
                if (truncate)
                    filestream.SetLength(0);
            }
            catch (IOException)
            {
                // TODO set $!
                return new P5Scalar(runtime, false);
            }

            // TODO handle encoding
            var handle = new P5Handle(
                runtime,
                is_read ? new StreamReader(filestream) : null,
                is_write ? new StreamWriter(filestream) : null);

            target.SetHandle(runtime, handle);

            return new P5Scalar(runtime, true);
        }

        public static P5Scalar Close(Runtime runtime, P5Scalar arg)
        {
            var handle = arg.DereferenceHandle(runtime);
            bool res = handle.Close(runtime);

            return new P5Scalar(runtime, res);
        }
    }
}
