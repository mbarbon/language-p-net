using Runtime = org.mbarbon.p.runtime.Runtime;
using System.Collections.Generic;
using StringSplitOptions = System.StringSplitOptions;
using Opcode = org.mbarbon.p.runtime.Opcode;
using Builtins = org.mbarbon.p.runtime.Builtins;
using Overloads = org.mbarbon.p.runtime.Overloads;
using Glue = org.mbarbon.p.runtime.NetGlue;
using Generator = org.mbarbon.p.runtime.Generator;
using Serializer = org.mbarbon.p.runtime.Serializer;

namespace org.mbarbon.p.values
{
    public class P5SymbolTable : P5Hash
    {
        public P5SymbolTable(Runtime runtime, string _name) : base(runtime)
        {
            name = _name;
        }

        public string GetName(Runtime runtime)
        {
            return name;
        }

        public P5Scalar GetScalar(Runtime runtime, string name, bool create)
        {
            var glob = GetGlob(runtime, name, true);
            if (glob.Scalar == null && create)
                glob.Scalar = new P5Scalar(runtime);

            return glob.Scalar;
        }

        public P5Scalar GetStashScalar(Runtime runtime, string name, bool create)
        {
            var glob = GetStashGlob(runtime, name, true);
            if (glob.Scalar == null && create)
                glob.Scalar = new P5Scalar(runtime);

            return glob.Scalar;
        }

        public P5Array GetArray(Runtime runtime, string name, bool create)
        {
            var glob = GetGlob(runtime, name, true);
            P5Array array;
            if ((array = glob.Array) == null && create)
                array = glob.Array = new P5Array(runtime);

            return array;
        }

        public P5Hash GetHash(Runtime runtime, string name, bool create)
        {
            var glob = GetGlob(runtime, name, true);
            P5Hash hash;
            if ((hash = glob.Hash) == null && create)
                hash = glob.Hash = new P5Hash(runtime);

            return hash;
        }

        public P5Hash GetStash(Runtime runtime, string name, bool create)
        {
            string[] packs = name.Split(separator, StringSplitOptions.None);
            P5SymbolTable st = GetPackage(runtime, packs, true, create);

            return st;
        }

        public P5Handle GetHandle(Runtime runtime, string name, bool create)
        {
            var glob = GetGlob(runtime, name, true);
            P5Handle handle;
            if ((handle = glob.Handle) == null && create)
                handle = glob.Handle = new P5Handle(runtime, null, null);

            return handle;
        }

        public P5Typeglob GetGlob(Runtime runtime, string name, bool create)
        {
            string[] packs = name.Split(separator, StringSplitOptions.None);
            P5SymbolTable st = GetPackage(runtime, packs, true, true);

            return st.GetStashGlob(runtime, packs[packs.Length - 1], create);
        }

        public P5Typeglob GetStashGlob(Runtime runtime, string name, bool create)
        {
            IP5Any value;
            if (!hash.TryGetValue(name, out value) && create)
            {
                P5Typeglob glob = new P5Typeglob(
                    runtime, this.name + "::" + name);
                ApplyMagic(runtime, name, glob);
                hash.Add(name, glob);
                value = glob;
            }

            return value as P5Typeglob;
        }

        public P5Code GetCode(Runtime runtime, string name, bool create)
        {
            P5Typeglob glob = GetGlob(runtime, name, true);
            P5Code code;
            if ((code = glob.Code) == null && create)
                code = glob.Code = new P5Code(name, null);

            return code;
        }

        public P5Code GetStashCode(Runtime runtime, string name, bool create)
        {
            P5Typeglob glob = GetStashGlob(runtime, name, true);
            P5Code code;
            if ((code = glob.Code) == null && create)
                code = glob.Code = new P5Code(name, null);

            return code;
        }

        public void SetCode(Runtime runtime, string name, P5Code code)
        {
            P5Typeglob glob = GetGlob(runtime, name, true);
            glob.Code = code;
        }

        public void DefineCode(Runtime runtime, string name, P5Code code)
        {
            P5Typeglob glob = GetGlob(runtime, name, true);

            if (glob.Code == null)
                glob.Code = code;
            else
                glob.Code.Assign(runtime, code);
        }

        public P5SymbolTable GetPackage(Runtime runtime, string pack)
        {
            string[] packs = pack.Split(separator, StringSplitOptions.None);

            return GetPackage(runtime, packs, false, true);
        }

        public P5SymbolTable GetPackage(Runtime runtime, string pack,
                                        bool create)
        {
            string[] packs = pack.Split(separator, StringSplitOptions.None);

            return GetPackage(runtime, packs, false, create);
        }

        virtual protected void ApplyMagic(Runtime runtime, string name,
                                          P5Typeglob symbol)
        {
            if (name.Length == 0 || name[0] == '0')
                return;
            for (int i = 0; i < name.Length; ++i)
                if (!char.IsDigit(name[i]))
                    return;

            symbol.Scalar = new P5Capture(runtime, int.Parse(name));
        }

        internal P5SymbolTable GetPackage(Runtime runtime, string pack,
                                          bool skip_last, bool create)
        {
            string[] packs = pack.Split(separator, StringSplitOptions.None);

            return GetPackage(runtime, packs, skip_last, create);
        }

        internal P5SymbolTable GetPackage(Runtime runtime, string[] packs,
                                          bool skip_last, bool create)
        {
            P5SymbolTable current = this;
            IP5Any value;

            int last = packs.Length + (skip_last ? -1 : 0);
            int first = 0;
            if (IsMain && last > 0 && (packs[0] == "main" || packs[0] == ""))
                first = 1;
            for (int i = first; i < last; ++i)
            {
                if (!current.hash.TryGetValue(packs[i] + "::", out value))
                {
                    if (!create)
                        return null;

                    var name = string.Join("::", packs, 0, i - first + 1);
                    P5Typeglob glob = new P5Typeglob(
                        runtime, string.Join("::", packs));
                    glob.Hash = new P5SymbolTable(runtime, name);
                    current.hash.Add(packs[i] + "::", glob);
                    value = glob;
                }
                current = (value as P5Typeglob).Hash as P5SymbolTable;
            }

            return current;
        }

        public P5Code FindMethod(Runtime runtime, string method, bool is_super)
        {
            var code = GetStashCode(runtime, method, false);
            if (code != null && !is_super)
                return code;

            IP5Any isa;
            P5Array isa_array = null;
            if (hash.TryGetValue("ISA", out isa))
                isa_array = (isa as P5Typeglob).Array;

            if (isa_array == null || isa_array.GetCount(runtime) == 0)
            {
                var universal = runtime.SymbolTable.Universal;

                // avoid infinite recursion when searching in UNIVERSAL
                if (this == universal)
                    return null;
                return universal.FindMethod(runtime, method, false);
            }

            foreach (var c in isa_array)
            {
                var c_str = c.AsString(runtime);
                var super = runtime.SymbolTable.GetPackage(runtime, c_str, false);
                if (super == null)
                    continue;

                code = super.FindMethod(runtime, method, false);
                if (code != null)
                    return code;
            }

            return null;
        }

        public bool IsDerivedFrom(Runtime runtime, P5SymbolTable parent)
        {
            if (this == parent)
                return true;

            IP5Any isa;
            P5Array isa_array = null;
            if (hash.TryGetValue("ISA", out isa))
                isa_array = (isa as P5Typeglob).Array;

            if (isa_array == null || isa_array.GetCount(runtime) == 0)
                return parent == runtime.SymbolTable.Universal;

            foreach (var e in isa_array)
            {
                var base_name = e.AsString(runtime);
                var base_stash = runtime.SymbolTable.GetPackage(runtime, base_name);

                if (base_stash == null)
                    continue;
                if (base_stash == parent || base_stash.IsDerivedFrom(runtime, parent))
                    return true;
            }

            return false;
        }

        public void SetOverloads(Overloads _overloads)
        {
            overloads = _overloads;
        }

        public virtual bool IsMain {
            get { return false; }
        }

        public bool HasOverloading {
            get { return overloads != null; }
        }

        public Overloads Overloads {
            get { return overloads; }
        }

        protected readonly string[] separator = new string [] {"::"};
        protected string name;
        protected Overloads overloads;
    }

    public class P5MainSymbolTable : P5SymbolTable
    {
        public P5MainSymbolTable(Runtime runtime, string name) : base(runtime, name)
        {
            var stdout = GetStashGlob(runtime, "STDOUT", true);
            stdout.Handle = new P5Handle(runtime, null, System.Console.Out);

            var stdin = GetStashGlob(runtime, "STDIN", true);
            stdin.Handle = new P5Handle(runtime, System.Console.In, null);

            var stderr = GetStashGlob(runtime, "STDERR", true);
            stderr.Handle = new P5Handle(runtime, null, System.Console.Error);

            var dquote = GetStashGlob(runtime, "\"", true);
            dquote.Scalar = new P5Scalar(runtime, " ");

            var version = GetStashGlob(runtime, "]", true);
            version.Scalar = new P5Scalar(runtime, 5.008);

            // UNIVERSAL
            universal = GetPackage(runtime, "UNIVERSAL", true);

            var isa = universal.GetStashGlob(runtime, "isa", true);
            isa.Code = new P5NativeCode("UNIVERSAL::isa", new P5Code.Sub(WrapIsa));

            // Internals
            var internals = GetPackage(runtime, "Internals", true);

            var add_overload = internals.GetStashGlob(runtime, "add_overload", true);
            add_overload.Code = new P5NativeCode("Internals::add_overload", new P5Code.Sub(WrapAddOverload));

            // Internals::Net (TODO move to external assembly)
            var internals_net = GetPackage(runtime, "Internals::Net", true);

            var get_class = internals_net.GetStashGlob(runtime, "get_class", true);
            get_class.Code = new P5NativeCode("Internals::Net::get_class", new P5Code.Sub(WrapGetClass));

            var specialize_type = internals_net.GetStashGlob(runtime, "specialize_type", true);
            specialize_type.Code = new P5NativeCode("Internals::Net::specialize_type", new P5Code.Sub(WrapSpecializeType));

            var create = internals_net.GetStashGlob(runtime, "create", true);
            create.Code = new P5NativeCode("Internals::Net::create", new P5Code.Sub(WrapCreate));

            var call_method = internals_net.GetStashGlob(runtime, "call_method", true);
            call_method.Code = new P5NativeCode("Internals::Net::call_mehtod", new P5Code.Sub(WrapCallMethod));

            var call_static = internals_net.GetStashGlob(runtime, "call_static", true);
            call_static.Code = new P5NativeCode("Internals::Net::call_static", new P5Code.Sub(WrapCallStatic));

            var get_property = internals_net.GetStashGlob(runtime, "get_property", true);
            get_property.Code = new P5NativeCode("Internals::Net::get_property", new P5Code.Sub(WrapGetProperty));

            var set_property = internals_net.GetStashGlob(runtime, "set_property", true);
            set_property.Code = new P5NativeCode("Internals::Net::set_property", new P5Code.Sub(WrapSetProperty));

            var extend = internals_net.GetStashGlob(runtime, "extend", true);
            extend.Code = new P5NativeCode("Internals::Net::extend", new P5Code.Sub(WrapExtend));

            var compile = internals_net.GetStashGlob(runtime, "compile_assembly", true);
            compile.Code = new P5NativeCode("Internals::Net::compile_assembly", new P5Code.Sub(WrapCompileAssembly));
        }

        private static IP5Any WrapIsa(Runtime runtime, Opcode.ContextValues context,
                                      P5ScratchPad pad, P5Array args)
        {
            var value = args.GetItem(runtime, 0) as P5Scalar;
            var parent = args.GetItem(runtime, 1);
            bool is_derived = Builtins.IsDerivedFrom(runtime, value, parent);

            return new P5Scalar(runtime, is_derived);
        }

        private static IP5Any WrapAddOverload(Runtime runtime, Opcode.ContextValues context,
                                              P5ScratchPad pad, P5Array args)
        {
            var pack = args.GetItem(runtime, 0) as P5Scalar;
            var opref = args.GetItem(runtime, 1) as P5Scalar;
            var ops = opref.DereferenceArray(runtime) as P5Array;

            Builtins.AddOverload(runtime, pack.AsString(runtime), ops);

            return null;
        }

        private static IP5Any WrapGetClass(Runtime runtime, Opcode.ContextValues context,
                                           P5ScratchPad pad, P5Array args)
        {
            var name = args.GetItem(runtime, 0);

            return Glue.GetClass(runtime, name.AsString(runtime));
        }

        private static IP5Any WrapSpecializeType(Runtime runtime, Opcode.ContextValues context,
                                                 P5ScratchPad pad, P5Array args)
        {
            var type = Glue.UnwrapValue<System.Type>(runtime, args.GetItem(runtime, 0));
            var tyargs = new System.Type[args.GetCount(runtime) - 1];

            for (int i = args.GetCount(runtime) - 1; i > 0; --i)
                tyargs[i - 1] = Glue.UnwrapValue<System.Type>(runtime, args.GetItem(runtime, i));

            return new P5Scalar(new P5NetWrapper(type.MakeGenericType(tyargs)));
        }

        private static IP5Any WrapCreate(Runtime runtime, Opcode.ContextValues context,
                                         P5ScratchPad pad, P5Array args)
        {
            var cls = args.GetItem(runtime, 0) as P5Scalar;
            int count = args.GetCount(runtime);
            var arg = new P5Scalar[count - 1];

            for (int i = 1; i < count; ++i)
                arg[i - 1] = args.GetItem(runtime, i) as P5Scalar;

            return Glue.CallConstructor(runtime, cls, arg);
        }

        private static IP5Any WrapCallMethod(Runtime runtime, Opcode.ContextValues context,
                                             P5ScratchPad pad, P5Array args)
        {
            var obj = args.GetItem(runtime, 0) as P5Scalar;
            var name = args.GetItem(runtime, 1).AsString(runtime);
            int count = args.GetCount(runtime);
            var arg = new P5Scalar[count - 2];

            for (int i = 2; i < count; ++i)
                arg[i - 2] = args.GetItem(runtime, i) as P5Scalar;

            return Glue.CallMethod(runtime, obj, name, arg);
        }

        private static IP5Any WrapCallStatic(Runtime runtime, Opcode.ContextValues context,
                                             P5ScratchPad pad, P5Array args)
        {
            var type = Glue.UnwrapValue<System.Type>(runtime, args.GetItem(runtime, 0));
            var name = args.GetItem(runtime, 1).AsString(runtime);
            int count = args.GetCount(runtime);
            var arg = new P5Scalar[count - 2];

            for (int i = 2; i < count; ++i)
                arg[i - 2] = args.GetItem(runtime, i) as P5Scalar;

            return Glue.CallStaticMethod(runtime, type, name, arg);
        }

        private static IP5Any WrapGetProperty(Runtime runtime, Opcode.ContextValues context,
                                              P5ScratchPad pad, P5Array args)
        {
            var obj = args.GetItem(runtime, 0) as P5Scalar;
            var name = args.GetItem(runtime, 1);

            return Glue.GetProperty(runtime, obj, name.AsString(runtime));
        }

        private static IP5Any WrapSetProperty(Runtime runtime, Opcode.ContextValues context,
                                              P5ScratchPad pad, P5Array args)
        {
            var obj = args.GetItem(runtime, 0) as P5Scalar;
            var name = args.GetItem(runtime, 1);
            var value = args.GetItem(runtime, 2);

            Glue.SetProperty(runtime, obj, name.AsString(runtime), value);

            return new P5Scalar(runtime);
        }

        private static IP5Any WrapExtend(Runtime runtime, Opcode.ContextValues context,
                                         P5ScratchPad pad, P5Array args)
        {
            var pack = args.GetItem(runtime, 0);
            var cls = args.GetItem(runtime, 1);

            return Glue.Extend(runtime, pack.AsString(runtime),
                               cls.AsString(runtime), "new", true);
        }

        private static IP5Any WrapCompileAssembly(Runtime runtime, Opcode.ContextValues context,
                                                  P5ScratchPad pad, P5Array args)
        {
            var asm_path = args.GetItem(runtime, 0).AsString(runtime);
            var generator = new Generator(runtime, asm_path);

            for (int i = 1; i < args.GetCount(runtime); ++i)
            {
                var arg = args.GetItem(runtime, i).AsString(runtime);
                string path, file;

                path = Builtins.SearchFile(runtime, arg);

                if (path != null)
                    file = arg;
                else
                {
                    file = arg.Replace("::", "/") + ".pm";
                    path = Builtins.SearchFile(runtime, file);
                }

                if (path == null)
                    throw new System.Exception(string.Format("File not found for '{0:S}'", arg));

                var cu = Serializer.ReadCompilationUnit(runtime, path);
                cu.FileName = file;

                generator.Generate(cu);
            }

            generator.Assembly.Save(new System.IO.FileInfo(asm_path + ".dll").Name);

            return new P5Scalar(runtime);
        }

        public override bool IsMain
        {
            get { return true; }
        }

        public P5SymbolTable Universal
        {
            get { return universal; }
        }

        private P5SymbolTable universal;
    }
}
