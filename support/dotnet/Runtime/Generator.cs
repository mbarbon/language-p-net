using org.mbarbon.p.values;

using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Linq.Expressions;
using System.Collections.Generic;
using Type = System.Type;

namespace org.mbarbon.p.runtime
{
    internal class ModuleGenerator
    {
        internal struct SubInfo
        {
            internal SubInfo(string method, Subroutine sub, FieldInfo codefield)
            {
                MethodName = method;
                SubName = sub.Name;
                IsMain = sub.Type == 1;
                Lexicals = sub.Lexicals;
                CodeField = codefield;
            }

            internal string MethodName;
            internal string SubName;
            internal LexicalInfo[] Lexicals;
            internal FieldInfo CodeField;
            internal bool IsMain;
        }

        public ModuleGenerator(TypeBuilder class_builder)
        {
            ClassBuilder = class_builder;
            Initializers = new List<Expression>();
            Subroutines = new List<SubInfo>();
            InitRuntime = Expression.Parameter(typeof(Runtime), "runtime");
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
            return AddField(initializer, typeof(Scalar));
        }

        public void AddSubInfo(Subroutine sub)
        {
            bool is_main = sub.Type == 1;
            string suffix = is_main          ? "main" :
                            sub.Name != null ? sub.Name :
                                               "anonymous";
            string method_name = "sub_" + suffix + "_" +
                                     MethodIndex++.ToString();
            FieldInfo field = ClassBuilder.DefineField(
                method_name + "_code", typeof(Code),
                FieldAttributes.Private|FieldAttributes.Static);

            Subroutines.Add(new SubInfo(method_name, sub, field));
        }

        public void AddMethod(int index, Subroutine sub)
        {
            var sg = new SubGenerator(this, Subroutines);
            var body = sg.Generate(sub, Subroutines[index].IsMain);

            MethodBuilder method_builder =
                ClassBuilder.DefineMethod(
                    Subroutines[index].MethodName,
                    MethodAttributes.Static|MethodAttributes.Public);
            body.CompileToMethod(method_builder);
        }

        public void AddInitMethod(FieldInfo main)
        {
            LabelTarget sub_label = Expression.Label(typeof(Code));
            MethodBuilder helper =
                ClassBuilder.DefineMethod(
                    "InitModule",
                    MethodAttributes.Public|MethodAttributes.Static,
                    typeof(void), new Type[] { typeof(Runtime) });
            Initializers.Add(Expression.Return(sub_label,
                                               Expression.Field(null, main),
                                               typeof(Code)));
            var constants_init =
                Expression.Lambda(
                    Expression.Label(
                        sub_label,
                        Expression.Block(Initializers)),
                        InitRuntime);

            constants_init.CompileToMethod(helper);
        }

        FieldInfo AddSubInitialization()
        {
            var code_ctor = typeof(Code).GetConstructor(
                new Type[] { typeof(Code.Sub), typeof(bool) });
            var get_method =
                typeof(Type).GetMethod(
                    "GetMethod", new Type[] { typeof(string) });
            var create_delegate =
                typeof(System.Delegate).GetMethod(
                    "CreateDelegate",
                    new Type[] { typeof(Type),
                                 typeof(MethodInfo) });
            var lexinfo_new_params = new Type[] {
                typeof(string), typeof(Opcode.Sigil), typeof(int),
                typeof(int), typeof(int), typeof(bool), typeof(bool),
            };
            var lexinfo_new = typeof(LexicalInfo).GetConstructor(lexinfo_new_params);
            var get_type =
                typeof(Type).GetMethod(
                    "GetType", new Type[] { typeof(string) });


            FieldInfo main = null;
            foreach (SubInfo si in Subroutines)
            {
                // new Code(System.Delegate.CreateDelegate(method, null)
                Expression initcode =
                    Expression.New(code_ctor, new Expression[] {
                            Expression.Call(
                                create_delegate,
                                Expression.Constant(typeof(Code.Sub)),
                                Expression.Call(
                                    Expression.Call(
                                        get_type,
                                        Expression.Constant(ClassBuilder.FullName)),
                                    get_method,
                                    Expression.Constant(si.MethodName))),
                            Expression.Constant(si.IsMain),
                        });

                Initializers.Add(
                    Expression.Assign(
                        Expression.Field(null, si.CodeField),
                        initcode));

                // code.ScratchPad = ScratchPad.CreateSubPad(lexicals,
                //                       main.ScratchPad)
                Expression[] alllex = new Expression[si.Lexicals.Length];
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
                            typeof(ScratchPad).GetMethod("CreateSubPad"),
                            lexicals,
                            main != null ?
                            (Expression)Expression.Property(
                                    Expression.Field(null, main),
                                    "ScratchPad") :
                            (Expression)Expression.Constant(null, typeof(ScratchPad))));
                Initializers.Add(init_pad);

                if (si.IsMain)
                {
                    // code.NewScope(runtime);
                    Expression set_main_pad =
                        Expression.Call(
                            Expression.Field(null, si.CodeField),
                            typeof(Code).GetMethod("NewScope"),
                            InitRuntime);

                    Initializers.Add(set_main_pad);
                    main = si.CodeField;
                }
                else if (si.SubName != null)
                {
                    // runtime.SymbolTable.SetCode(runtime, sub_name, code)
                    Expression add_to_symboltable = 
                        Expression.Call(
                            Expression.Field(
                                InitRuntime,
                                typeof(Runtime).GetField("SymbolTable")),
                            typeof(SymbolTable).GetMethod("SetCode"),
                            InitRuntime,
                            Expression.Constant(si.SubName),
                            Expression.Field(null, si.CodeField));
                    Initializers.Add(add_to_symboltable);
                }
            }

            return main;
        }

        public Code CompleteGeneration(Runtime runtime)
        {
            FieldInfo main = AddSubInitialization();
            AddInitMethod(main);

            Type mod = ClassBuilder.CreateType();
            object main_sub = mod.GetMethod("InitModule")
                                  .Invoke(null, new object[] { runtime });

            return (Code)main_sub;
        }

        private TypeBuilder ClassBuilder;
        private List<Expression> Initializers;
        private List<SubInfo> Subroutines;
        private int MethodIndex = 0;
        public ParameterExpression InitRuntime;
    }

    internal class SubGenerator
    {
        public SubGenerator(ModuleGenerator module_generator,
                            List<ModuleGenerator.SubInfo> subroutines)
        {
            Runtime = Expression.Parameter(typeof(Runtime), "runtime");
            Arguments = Expression.Parameter(typeof(org.mbarbon.p.values.Array), "args");
            Context = Expression.Parameter(typeof(Opcode.Context), "context");
            Pad = Expression.Parameter(typeof(ScratchPad), "pad");
            Variables = new List<ParameterExpression>();
            Lexicals = new List<ParameterExpression>();
            BlockLabels = new List<LabelTarget>();
            Blocks = new List<Expression>();
            ModuleGenerator = module_generator;
            Subroutines = subroutines;
        }

        private ParameterExpression GetVariable(int index)
        {
            while (Variables.Count <= index)
                Variables.Add(Expression.Variable(typeof(IAny)));

            return Variables[index];
        }

        private ParameterExpression GetLexical(int index, Opcode.Sigil slot)
        {
            while (Lexicals.Count <= index)
                Lexicals.Add(null);
            if (Lexicals[index] == null)
                Lexicals[index] =
                    Expression.Variable(slot == Opcode.Sigil.SCALAR ? typeof(Scalar) :
                                     slot == Opcode.Sigil.ARRAY  ? typeof(Array) :
                                     slot == Opcode.Sigil.HASH   ? typeof(Hash) :
                                                                typeof(void));

            return Lexicals[index];
        }

        private Expression GetLexicalPad(LexicalInfo info)
        {
            return GetLexicalPad(info, false);
        }

        private Expression GetLexicalPad(LexicalInfo info, bool writable)
        {
            var item =
                Expression.MakeIndex(
                    Pad, Pad.Type.GetProperty("Item"),
                    new Expression[] { Expression.Constant(info.Index) });

            if (writable)
                return item;

            if (info.Slot == Opcode.Sigil.SCALAR)
                return Expression.Convert(item, typeof(Scalar));
            else if (info.Slot == Opcode.Sigil.ARRAY)
                return Expression.Convert(item, typeof(Array));
            else if (info.Slot == Opcode.Sigil.HASH)
                return Expression.Convert(item, typeof(Hash));
            else
                throw new System.Exception("Invalid slot " + info.Slot.ToString());
        }

        public LambdaExpression Generate(Subroutine sub, bool is_main)
        {
            IsMain = is_main;
            SubLabel = Expression.Label(typeof(IAny));

            for (int i = 0; i < sub.BasicBlocks.Length; ++i)
                BlockLabels.Add(Expression.Label());
            for (int i = 0; i < sub.BasicBlocks.Length; ++i)
            {
                List<Expression> exps = new List<Expression>();
                exps.Add(Expression.Label(BlockLabels[i]));
                Generate(sub.BasicBlocks[i], exps);
                Blocks.Add(Expression.Block(typeof(IAny), exps));
            }
            // remove the dummy entry for @_ if present
            if (Lexicals.Count > 0 && Lexicals[0] == null)
                Lexicals.RemoveAt(0);
            if (Lexicals.Count != 0)
            {
                List<Expression> init_exps = new List<Expression>();
                foreach (var lex in Lexicals)
                {
                    init_exps.Add(Expression.Assign(lex, Expression.New(lex.Type.GetConstructor(new Type[] { typeof(Runtime) }),
                                                                 Runtime)));
                }
                Blocks.Insert(0, Expression.Block(typeof(void), init_exps));
            }

            Variables.InsertRange(0, Lexicals);

            var block = Expression.Block(typeof(IAny), Variables, Blocks);
            var args = new ParameterExpression[] { Runtime, Context, Pad, Arguments };
            return Expression.Lambda<Code.Sub>(Expression.Label(SubLabel, block), args);
        }

        public void Generate(BasicBlock bb, List<Expression> expressions)
        {
            foreach (var o in bb.Opcodes)
            {
                expressions.Add(Generate(o));
            }
        }

        public Expression Generate(Opcode op)
        {
            switch(op.Number)
            {
            case Opcode.OpNumber.OP_FRESH_STRING:
            case Opcode.OpNumber.OP_CONSTANT_STRING:
            {
                var ctor = typeof(Scalar).GetConstructor(new Type[] { typeof(Runtime), typeof(string) });
                return Expression.New(ctor, new Expression[] { Runtime, Expression.Constant(((ConstantString)op).Value) });
            }
            case Opcode.OpNumber.OP_CONSTANT_UNDEF:
            {
                var ctor = typeof(Scalar).GetConstructor(new Type[] { typeof(Runtime) });
                return Expression.New(ctor, new Expression[] { Runtime });
            }
            case Opcode.OpNumber.OP_CONSTANT_INTEGER:
            {
                var ctor = typeof(Scalar).GetConstructor(new Type[] { typeof(Runtime), typeof(int) });
                var init = Expression.New(ctor, new Expression[] { ModuleGenerator.InitRuntime, Expression.Constant(((ConstantInt)op).Value) });
                FieldInfo field = ModuleGenerator.AddField(init);

                return Expression.Field(null, field);
            }
            case Opcode.OpNumber.OP_CONSTANT_SUB:
            {
                ConstantSub cs = (ConstantSub)op;

                return Expression.Field(null, Subroutines[cs.Index].CodeField);
            }
            case Opcode.OpNumber.OP_GLOBAL:
            {
                Global gop = (Global)op;
                var st = typeof(Runtime).GetField("SymbolTable");
                string name = null;
                switch (gop.Slot)
                {
                case Opcode.Sigil.SCALAR:
                    name = "GetOrCreateScalar";
                    break;
                case Opcode.Sigil.ARRAY:
                    name = "GetOrCreateArray";
                    break;
                case Opcode.Sigil.HASH:
                    name = "GetOrCreateHash";
                    break;
                case Opcode.Sigil.SUB:
                    name = "GetCode";
                    break;
                case Opcode.Sigil.GLOB:
                    name = "GetOrCreateGlob";
                    break;
                case Opcode.Sigil.HANDLE:
                    name = "GetOrCreateHandle";
                    break;
                default:
                    throw new System.Exception(string.Format("Unhandled slot {0:D}", gop.Slot));
                }
                var gets = typeof(SymbolTable).GetMethod(name);
                return Expression.Call(Expression.Field(Runtime, st), gets, Runtime, Expression.Constant(gop.Name));
            }
            case Opcode.OpNumber.OP_MAKE_LIST:
            {
                List<Expression> data = new List<Expression>();
                foreach (var i in op.Childs)
                    data.Add(Generate(i));
                return Expression.New(typeof(org.mbarbon.p.values.List).GetConstructor(new Type[] {typeof(Runtime), typeof(IAny[])}),
                                    new Expression[] { Runtime, Expression.NewArrayInit(typeof(IAny), data) });
            }
            case Opcode.OpNumber.OP_PRINT:
            {
                return Expression.Call(typeof(Builtins).GetMethod("Print"), Runtime,
                                     Expression.Convert(Generate(op.Childs[0]), typeof(org.mbarbon.p.values.List)));
            }
            case Opcode.OpNumber.OP_END:
            {
                return Expression.Return(SubLabel, Expression.Constant(null, typeof(IAny)), typeof(IAny));
            }
            case Opcode.OpNumber.OP_RETURN:
            {
                Expression empty = Expression.New(typeof(org.mbarbon.p.values.List).GetConstructor(new Type[] { typeof(Runtime) }), Runtime);
                if (op.Childs.Length == 0)
                {
                    return Expression.Return(SubLabel, empty, typeof(IAny));
                }
                else
                {
                    ParameterExpression val = Expression.Variable(typeof(IAny), "ret");
                    Expression assign = Expression.Assign(val, Generate(op.Childs[0]));
                    Expression iflist =
                        Expression.Condition(Expression.Equal(Context, Expression.Constant(Opcode.Context.LIST)),
                                           val, empty, typeof(IAny));
                    Expression retscalar =
                        Expression.Call(val, typeof(IAny).GetMethod("AsScalar"), Runtime);
                    Expression ifscalar =
                        Expression.Condition(Expression.Equal(Context, Expression.Constant(Opcode.Context.SCALAR)),
                                           retscalar, iflist, typeof(IAny));

                    return Expression.Return(SubLabel, Expression.Block(typeof(IAny), new ParameterExpression[] { val },
                                                                   new Expression[] { assign, ifscalar }),
                                           typeof(IAny));
                }
            }
            case Opcode.OpNumber.OP_ASSIGN:
            {
                return Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("Assign"), Runtime,
                                     Generate(op.Childs[1]));
            }
            case Opcode.OpNumber.OP_GET:
            {
                return GetVariable(((GetSet)op).Variable);
            }
            case Opcode.OpNumber.OP_SET:
            {
                return Expression.Assign(GetVariable(((GetSet)op).Variable),
                                       Generate(op.Childs[0]));
            }
            case Opcode.OpNumber.OP_JUMP:
            {
                return Expression.Goto(BlockLabels[((Jump)op).To], typeof(IAny));
            }
            case Opcode.OpNumber.OP_JUMP_IF_S_EQ:
            {
                Expression cmp = Expression.Equal(Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("AsString"), Runtime),
                                               Expression.Call(Generate(op.Childs[1]), typeof(IAny).GetMethod("AsString"), Runtime));
                Expression jump = Expression.Goto(BlockLabels[((Jump)op).To], typeof(IAny));

                return Expression.IfThen(cmp, jump);
            }
            case Opcode.OpNumber.OP_JUMP_IF_F_EQ:
            {
                Expression cmp = Expression.Equal(Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("AsFloat"), Runtime),
                                               Expression.Call(Generate(op.Childs[1]), typeof(IAny).GetMethod("AsFloat"), Runtime));
                Expression jump = Expression.Goto(BlockLabels[((Jump)op).To], typeof(IAny));

                return Expression.IfThen(cmp, jump);
            }
            case Opcode.OpNumber.OP_JUMP_IF_F_GE:
            {
                Expression cmp = Expression.GreaterThanOrEqual(Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("AsFloat"), Runtime),
                                                           Expression.Call(Generate(op.Childs[1]), typeof(IAny).GetMethod("AsFloat"), Runtime));
                Expression jump = Expression.Goto(BlockLabels[((Jump)op).To], typeof(IAny));

                return Expression.IfThen(cmp, jump);
            }
            case Opcode.OpNumber.OP_JUMP_IF_TRUE:
            {
                Expression cmp = Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("AsBoolean"), Runtime);
                Expression jump = Expression.Goto(BlockLabels[((Jump)op).To], typeof(IAny));

                return Expression.IfThen(cmp, jump);
            }
            case Opcode.OpNumber.OP_LOG_NOT:
            {
                return Expression.New(typeof(Scalar).GetConstructor(new Type[] { typeof(Runtime), typeof(bool) }), Runtime,
                                      Expression.Not(Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("AsBoolean"), Runtime)));
            }
            case Opcode.OpNumber.OP_DEFINED:
            {
                return Expression.New(typeof(Scalar).GetConstructor(new Type[] { typeof(Runtime), typeof(bool) }), Runtime,
                                    Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("IsDefined"), Runtime));
            }
            case Opcode.OpNumber.OP_CONCAT:
            {
                Expression s1 = Expression.Call(Generate(op.Childs[0]),
                                             typeof(IAny).GetMethod("AsString"), Runtime);
                Expression s2 = Expression.Call(Generate(op.Childs[1]),
                                             typeof(IAny).GetMethod("AsString"), Runtime);
                return Expression.New(typeof(Scalar).GetConstructor(new Type[] { typeof(Runtime), typeof(string) }), Runtime,
                                    Expression.Call(typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }), s1, s2));
            }
            case Opcode.OpNumber.OP_CONCAT_ASSIGN:
            {
                return Expression.Call(Expression.Convert(Generate(op.Childs[0]), typeof(Scalar)),
                                     typeof(IAny).GetMethod("ConcatAssign"), Runtime, Generate(op.Childs[1]));
            }
            case Opcode.OpNumber.OP_ARRAY_LENGTH:
            {
                Expression len = Expression.Call(Expression.Convert(Generate(op.Childs[0]), typeof(org.mbarbon.p.values.Array)),
                                              typeof(org.mbarbon.p.values.Array).GetMethod("GetCount"),
                                              Runtime);
                Expression len_1 = Expression.Subtract(len, Expression.Constant(1));
                return Expression.New(typeof(Scalar).GetConstructor(new Type[] {typeof(Runtime), typeof(int)}),
                                    new Expression[] { Runtime, len_1 });
            }
            case Opcode.OpNumber.OP_ADD:
            {
                Expression sum = Expression.Add(Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("AsFloat"), Runtime),
                                             Expression.Call(Generate(op.Childs[1]), typeof(IAny).GetMethod("AsFloat"), Runtime));
                return Expression.New(typeof(Scalar).GetConstructor(new Type[] {typeof(Runtime), typeof(double)}),
                                    new Expression[] { Runtime, sum });
            }
            case Opcode.OpNumber.OP_SUBTRACT:
            {
                Expression sum = Expression.Subtract(Expression.Call(Generate(op.Childs[0]), typeof(IAny).GetMethod("AsFloat"), Runtime),
                                                  Expression.Call(Generate(op.Childs[1]), typeof(IAny).GetMethod("AsFloat"), Runtime));
                return Expression.New(typeof(Scalar).GetConstructor(new Type[] {typeof(Runtime), typeof(double)}),
                                    new Expression[] { Runtime, sum });
            }
            case Opcode.OpNumber.OP_ARRAY_ELEMENT:
            {
                return Expression.Call(Generate(op.Childs[1]), typeof(Array).GetMethod("GetItemOrUndef"),
                                     Runtime, Generate(op.Childs[0]));
            }
            case Opcode.OpNumber.OP_HASH_ELEMENT:
            {
                return Expression.Call(Generate(op.Childs[1]), typeof(Hash).GetMethod("GetItemOrUndef"),
                                     Runtime, Generate(op.Childs[0]));
            }
            case Opcode.OpNumber.OP_LEXICAL:
            {
                Lexical lx = (Lexical)op;

                return lx.Index == 0 && !IsMain ? Arguments : GetLexical(lx.Index, lx.Slot);
            }
            case Opcode.OpNumber.OP_LEXICAL_CLEAR:
            {
                Lexical lx = (Lexical)op;
                Expression lexvar = GetLexical(lx.Index, lx.Slot);

                return Expression.Assign(lexvar, Expression.Constant(null, lexvar.Type));
            }
            case Opcode.OpNumber.OP_LEXICAL_PAD:
            {
                Lexical lx = (Lexical)op;

                return GetLexicalPad(lx.LexicalInfo);
            }
            case Opcode.OpNumber.OP_LEXICAL_PAD_CLEAR:
            {
                Lexical lx = (Lexical)op;
                Expression lexvar = GetLexicalPad(lx.LexicalInfo, true);

                return Expression.Assign(lexvar, Expression.Constant(null, lexvar.Type));
            }
            case Opcode.OpNumber.OP_CALL:
            {
                return Expression.Call(Generate(op.Childs[1]), typeof(Code).GetMethod("Call"),
                                     Runtime, Context, Generate(op.Childs[0]));
            }
            case Opcode.OpNumber.OP_DEREFERENCE_SUB:
            {
                return Expression.Call(
                           Generate(op.Childs[0]),
                           typeof(IAny).GetMethod("DereferenceSubroutine"),
                           Runtime);
            }
            case Opcode.OpNumber.OP_MAKE_CLOSURE:
            {
                return Expression.Call(
                           Generate(op.Childs[0]),
                           typeof(Code).GetMethod("MakeClosure"),
                           Runtime, Pad);
            }
            default:
                throw new System.Exception(string.Format("Unhandled opcode {0:S}", op.Number.ToString()));
            }
        }

        private LabelTarget SubLabel;
        private ParameterExpression Runtime, Arguments, Context, Pad;
        private List<ParameterExpression> Variables, Lexicals;
        private List<LabelTarget> BlockLabels;
        private List<Expression> Blocks;
        private bool IsMain;
        private ModuleGenerator ModuleGenerator;
        private List<ModuleGenerator.SubInfo> Subroutines;
    }

    public class Generator
    {
        public Generator(Runtime r)
        {
            Runtime = r;
        }

        public Code Generate(string assembly_name, CompilationUnit cu)
        {
            var file = new System.IO.FileInfo(cu.FileName);
            AssemblyName asm_name = new AssemblyName(assembly_name != null ? assembly_name : file.Name);
            AssemblyBuilder asm_builder =
                System.AppDomain.CurrentDomain.DefineDynamicAssembly(asm_name,
                                                              AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mod_builder = asm_builder.DefineDynamicModule(asm_name.Name, asm_name.Name + ".dll");
            // FIXME should at least be the module name with which the file was loaded, in case
            //       multiple modules are compiled to the same file; works for now
            TypeBuilder perl_module = mod_builder.DefineType(file.Name, TypeAttributes.Public);
            ModuleGenerator perl_mod_generator = new ModuleGenerator(perl_module);

            for (int i = 0; i < cu.Subroutines.Length; ++i)
                perl_mod_generator.AddSubInfo(cu.Subroutines[i]);
            for (int i = 0; i < cu.Subroutines.Length; ++i)
                perl_mod_generator.AddMethod(i, cu.Subroutines[i]);

            return perl_mod_generator.CompleteGeneration(Runtime);
        }

        Runtime Runtime;
    }
}
