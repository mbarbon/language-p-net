using org.mbarbon.p.values;

using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Scripting.Ast;
using System.Collections.Generic;
using Type = System.Type;
using DebugInfoGenerator = System.Runtime.CompilerServices.DebugInfoGenerator;
using MemoryStream = System.IO.MemoryStream;
using BinaryFormatter = System.Runtime.Serialization.Formatters.Binary.BinaryFormatter;

namespace org.mbarbon.p.runtime
{
    internal class StaticModuleGenerator
    {
        internal struct SubInfo
        {
            internal SubInfo(string method, Subroutine sub, FieldInfo codefield)
            {
                MethodName = method;
                CodeField = codefield;
                Subroutine = sub;
            }

            internal Subroutine Subroutine;
            internal string MethodName;
            internal FieldInfo CodeField;

            internal string SubName
            {
                get { return Subroutine.Name; }
            }

            internal int[] Prototype
            {
                get { return Subroutine.Prototype; }
            }

            internal List<LexicalInfo> Lexicals
            {
                get { return Subroutine.Lexicals; }
            }

            internal bool IsMain
            {
                get { return Subroutine.IsMain; }
            }
        }

        public StaticModuleGenerator(TypeBuilder class_builder, bool native_regex)
        {
            ClassBuilder = class_builder;
            NativeRegex = native_regex;
            Initializers = new List<Expression>();
            Subroutines = new Dictionary<Subroutine, SubInfo>();
            CreatedPackages = new HashSet<string>();
            InitRuntime = Expression.Parameter(typeof(Runtime), "runtime");
            Globals = new Dictionary<string, FieldInfo>();
        }

        public FieldInfo AddField(Expression initializer, Type type)
        {
            string field_name = "const_" + Initializers.Count.ToString();
            FieldInfo field =
                ClassBuilder.DefineField(
                    field_name, type,
                    FieldAttributes.Private|FieldAttributes.Static);
            var init =
                Expression.Assign(
                    Expression.Field(null, field),
                    initializer);
            Initializers.Add(init);

            return field;
        }

        public FieldInfo AddField(Expression initializer)
        {
            return AddField(initializer, typeof(P5Scalar));
        }

        public void AddInitPackage(string name)
        {
            if (CreatedPackages.Contains(name))
                return;
            CreatedPackages.Add(name);

            var create_package = Expression.Call(
                Expression.Field(
                    InitRuntime,
                    typeof(Runtime).GetField("SymbolTable")),
                typeof(P5SymbolTable).GetMethod("GetPackage", new System.Type[] { typeof(Runtime), typeof(string), typeof(bool) }),
                InitRuntime,
                Expression.Constant(name),
                Expression.Constant(true));

            Initializers.Add(create_package);
        }

        public void AddRegexInfo(Subroutine sub)
        {
            FieldInfo field = ClassBuilder.DefineField(
                "regex_" + MethodIndex++.ToString(), typeof(IP5Regex),
                FieldAttributes.Private|FieldAttributes.Static);

            Subroutines[sub] = new SubInfo(null, sub, field);
        }

        public void AddSubInfo(Subroutine sub)
        {
            bool is_main = sub.IsMain;
            string suffix = is_main          ? "main" :
                            sub.Name != null ? sub.Name :
                                               "anonymous";
            string method_name = "sub_" + suffix + "_" +
                                     MethodIndex++.ToString();
            FieldInfo field = ClassBuilder.DefineField(
                method_name + "_code", typeof(P5Code),
                FieldAttributes.Private|FieldAttributes.Static);

            Subroutines[sub] = new SubInfo(method_name, sub, field);
        }

        public void AddRegex(Subroutine sub)
        {
            IP5Regex regex = NativeRegex ? RegexGenerator.GenerateNetRegex(sub) : RegexGenerator.GenerateRegex(sub);
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();

            formatter.Serialize(stream, regex);

            var bytes = new List<Expression>();
            foreach (var b in stream.ToArray())
                bytes.Add(Expression.Constant(b));

            var byteField = AddField(
                Expression.NewArrayInit(typeof(byte), bytes),
                typeof(byte[]));
            var memStream = Expression.New(
                typeof(MemoryStream).GetConstructor(
                    new Type[] { typeof(byte[]) }),
                Expression.Field(null, byteField));
            var deserializer = Expression.New(
                typeof(BinaryFormatter).GetConstructor(
                    new Type[0]));
            var init = Expression.Convert(
                Expression.Call(
                    deserializer,
                    typeof(BinaryFormatter).GetMethod(
                        "Deserialize",
                        new Type[] { typeof(System.IO.Stream) }),
                    memStream),
                typeof(IP5Regex));

            Initializers.Add(
                Expression.Assign(
                    Expression.Field(null, Subroutines[sub].CodeField),
                    init));
        }

        public void AddMethod(Subroutine sub)
        {
            var sg = new StaticSubGenerator(this, Subroutines);
            var body = sg.Generate(sub, Subroutines[sub].IsMain);

            MethodBuilder method_builder =
                ClassBuilder.DefineMethod(
                    Subroutines[sub].MethodName,
                    MethodAttributes.Static|MethodAttributes.Public);
            body.CompileToMethod(method_builder,
                                 DebugInfoGenerator.CreatePdbGenerator());
        }

        public void AddInitMethod(FieldInfo main)
        {
            LabelTarget sub_label = Expression.Label(typeof(P5Code));
            MethodBuilder helper =
                ClassBuilder.DefineMethod(
                    "InitModule",
                    MethodAttributes.Public|MethodAttributes.Static,
                    typeof(void), new Type[] { typeof(Runtime) });
            Initializers.Add(Expression.Return(sub_label,
                                               Expression.Field(null, main),
                                               typeof(P5Code)));
            var constants_init =
                Expression.Lambda(
                    Expression.Label(
                        sub_label,
                        Expression.Block(Initializers)),
                        InitRuntime);

            constants_init.CompileToMethod(helper);
        }

        FieldInfo AddSubInitialization(Subroutine[] subroutines,
                                       bool anonymous, FieldInfo main)
        {
            var code_ctor = typeof(P5Code).GetConstructor(
                new[] { typeof(string), typeof(int[]), typeof(System.Type), typeof(string), typeof(bool) });
            var lexinfo_new_params = new Type[] {
                typeof(string), typeof(Opcode.Sigil), typeof(int),
                typeof(int), typeof(int), typeof(bool), typeof(bool),
            };
            var lexinfo_new = typeof(LexicalInfo).GetConstructor(lexinfo_new_params);
            var get_type =
                typeof(Type).GetMethod(
                    "GetType", new Type[] { typeof(string) });

            foreach (var sub in subroutines)
            {
                var si = Subroutines[sub];
                if (si.Subroutine.IsRegex)
                    continue;
                if (anonymous != (si.SubName == null))
                    continue;

                // new P5Code(System.Delegate.CreateDelegate(method, null)
                Expression proto;
                if (si.Prototype == null)
                    proto = Expression.Constant(null, typeof(int[]));
                else if (   si.Prototype.Length == 3
                         && si.Prototype[0] == 0
                         && si.Prototype[1] == 0
                         && si.Prototype[2] == 0)
                    proto = Expression.Field(null, typeof(P5Code).GetField("EMPTY_PROTO"));
                else
                {
                    Expression[] values = new Expression[si.Prototype.Length];

                    for (int i = 0; i < si.Prototype.Length; ++i)
                        values[i] = Expression.Constant(si.Prototype[i]);

                    proto = Expression.NewArrayInit(typeof(int), values);
                }

                Expression initcode =
                    Expression.New(code_ctor, new Expression[] {
                            Expression.Constant(si.SubName ?? "ANONCODE"),
                            proto,
                            Expression.Call(
                                get_type,
                                Expression.Constant(ClassBuilder.FullName)),
                            Expression.Constant(si.MethodName),
                            Expression.Constant(si.IsMain),
                        });

                Initializers.Add(
                    Expression.Assign(
                        Expression.Field(null, si.CodeField),
                        initcode));

                // code.ScratchPad = P5ScratchPad.CreateSubPad(runtime,
                //                       lexicals, main.ScratchPad)
                Expression[] alllex = new Expression[si.Lexicals.Count];
                for (int i = 0; i < alllex.Length; ++i)
                {
                    LexicalInfo lex = si.Lexicals[i];

                    alllex[i] = Expression.New(
                        lexinfo_new,
                        new Expression[] {
                            Expression.Constant(lex.Name),
                            Expression.Constant(lex.Slot),
                            Expression.Constant(lex.Level),
                            Expression.Constant(lex.Index),
                            Expression.Constant(lex.OuterIndex),
                            Expression.Constant(lex.InPad),
                            Expression.Constant(lex.FromMain),
                        });
                }
                Expression lexicals =
                    Expression.NewArrayInit(typeof(LexicalInfo), alllex);
                Expression init_pad =
                    Expression.Assign(
                        Expression.Property(
                            Expression.Field(null, si.CodeField),
                            "ScratchPad"),
                        Expression.Call(
                            typeof(P5ScratchPad).GetMethod("CreateSubPad"),
                            InitRuntime,
                            lexicals,
                            main != null ?
                            (Expression)Expression.Property(
                                    Expression.Field(null, main),
                                    "ScratchPad") :
                            (Expression)Expression.Constant(null, typeof(P5ScratchPad))));
                Initializers.Add(init_pad);

                if (si.IsMain)
                {
                    // code.NewScope(runtime);
                    Expression set_main_pad =
                        Expression.Call(
                            Expression.Field(null, si.CodeField),
                            typeof(P5Code).GetMethod("NewScope"),
                            InitRuntime);

                    Initializers.Add(set_main_pad);
                    main = si.CodeField;
                }
                else if (   si.SubName != null
                         && (   si.SubName == "BEGIN"
                             || si.SubName.EndsWith("::BEGIN")))
                {
                    Expression empty_list =
                        Expression.New(
                            typeof(P5Array).GetConstructor(
                                new Type[] { typeof(Runtime) }),
                            InitRuntime);
                    Expression call_begin =
                        Expression.Call(
                            Expression.Field(null, si.CodeField),
                            typeof(P5Code).GetMethod("Call"),
                            InitRuntime,
                            Expression.Constant(Opcode.ContextValues.VOID),
                            empty_list);

                    Initializers.Add(call_begin);
                }
                else if (si.SubName != null)
                {
                    // runtime.SymbolTable.SetCode(runtime, sub_name, code)
                    Expression add_to_symboltable =
                        Expression.Call(
                            Expression.Field(
                                InitRuntime,
                                typeof(Runtime).GetField("SymbolTable")),
                            typeof(P5SymbolTable).GetMethod("DefineCode"),
                            InitRuntime,
                            Expression.Constant(si.SubName),
                            Expression.Field(null, si.CodeField));
                    Initializers.Add(add_to_symboltable);
                }
            }

            return main;
        }

        public Type CompleteGeneration(Subroutine[] subroutines,
                                       Runtime runtime)
        {
            // force generation of anonymous subroutine templates before all
            // other subroutines
            FieldInfo main = AddSubInitialization(subroutines, true, null);
            AddSubInitialization(subroutines, false, main);

            AddInitMethod(main);

            return ClassBuilder.CreateType();
        }

        public FieldInfo AccessGlob(string name, Expression initializer)
        {
            FieldInfo res;

            if (Globals.TryGetValue(name, out res))
                return res;

            res = AddField(initializer, typeof(P5Typeglob));
            Globals[name] = res;

            return res;
        }

        private Dictionary<string, FieldInfo> Globals;
        private TypeBuilder ClassBuilder;
        private bool NativeRegex;
        private List<Expression> Initializers;
        private Dictionary<Subroutine, SubInfo> Subroutines;
        private int MethodIndex = 0;
        private HashSet<string> CreatedPackages;
        public ParameterExpression InitRuntime;
    }
}
