using org.mbarbon.p.values;

using System; // Func
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Ast;
using System.Collections.Generic;
using Type = System.Type;
using IEnumerator = System.Collections.IEnumerator;

namespace org.mbarbon.p.runtime
{
    internal abstract class SubGenerator
    {
        private static Opcode NullOpcode;

        static SubGenerator()
        {
            NullOpcode = new Opcode();
            NullOpcode.Childs = new Opcode[0];
        }

        private static Type[] ProtoRuntime =
            new Type[] { typeof(Runtime) };
        private static Type[] ProtoRuntimeString =
            new Type[] { typeof(Runtime), typeof(string) };
        private static Type[] ProtoRuntimeInt =
            new Type[] { typeof(Runtime), typeof(int) };
        private static Type[] ProtoRuntimeBool =
            new Type[] { typeof(Runtime), typeof(bool) };
        private static Type[] ProtoRuntimeAny =
            new Type[] { typeof(Runtime), typeof(IP5Any) };
        private static Type[] ProtoInt =
            new Type[] { typeof(int) };

        public SubGenerator()
        {
            Runtime = Expression.Parameter(typeof(Runtime), "runtime");
            Arguments = Expression.Parameter(typeof(object), "args");
            Context = Expression.Parameter(typeof(Opcode.ContextValues), "context");
            Pad = Expression.Parameter(typeof(P5ScratchPad), "pad");
            Variables = new List<ParameterExpression>();
            Lexicals = new List<ParameterExpression>();
            Temporaries = new List<ParameterExpression>();
            BlockLabels = new Dictionary<BasicBlock, LabelTarget>();
            ValueBlocks = new Dictionary<BasicBlock, Expression>();
            LexStates = new List<ParameterExpression>();
            RxStates = new List<ParameterExpression>();
        }

        private Expression OpContext(Opcode op)
        {
            if (op.Context == (int)Opcode.ContextValues.CALLER)
                return Context;
            else
                return Expression.Constant(
                    (Opcode.ContextValues)op.Context,
                    typeof(Opcode.ContextValues));
        }

        private ParameterExpression GetVariable(int index, Type type)
        {
            if (typeof(P5Scalar).IsAssignableFrom(type))
                type = typeof(IP5Any);
            while (Variables.Count <= index)
                Variables.Add(null);
            if (Variables[index] == null)
                Variables[index] = Expression.Variable(type);
            else if(Variables[index].Type != type)
                throw new System.Exception("Inconsistent types");

            return Variables[index];
        }

        private Type TypeForTemporary(Opcode.Sigil slot)
        {
            return slot == Opcode.Sigil.SCALAR   ? typeof(P5Scalar) :
                   slot == Opcode.Sigil.INDEXABLE? typeof(IP5Array) :
                   slot == Opcode.Sigil.HASH     ? typeof(P5Hash) :
                   slot == Opcode.Sigil.ITERATOR ? typeof(IEnumerator) :
                   slot == Opcode.Sigil.GLOB     ? typeof(P5Typeglob) :
                   slot == Opcode.Sigil.SUB      ? typeof(P5Code) :
                   slot == Opcode.Sigil.HANDLE   ? typeof(P5Handle) :
                   slot == Opcode.Sigil.ARRAY    ? typeof(List<object>) :
                                                   typeof(void);
        }

        private Type TypeForSlot(Opcode.Sigil slot)
        {
            return slot == Opcode.Sigil.SCALAR   ? typeof(P5Scalar) :
                   slot == Opcode.Sigil.INDEXABLE? typeof(IP5Array) :
                   slot == Opcode.Sigil.HASH     ? typeof(P5Hash) :
                   slot == Opcode.Sigil.ITERATOR ? typeof(IEnumerator) :
                   slot == Opcode.Sigil.GLOB     ? typeof(P5Typeglob) :
                   slot == Opcode.Sigil.SUB      ? typeof(P5Code) :
                   slot == Opcode.Sigil.HANDLE   ? typeof(P5Handle) :
                   slot == Opcode.Sigil.ARRAY    ? typeof(P5Array) :
                                                   typeof(void);
        }

        protected string MethodForSlot(Opcode.Sigil slot)
        {
            switch (slot)
            {
            case Opcode.Sigil.SCALAR:
                return "GetScalar";
            case Opcode.Sigil.ARRAY:
                return "GetArray";
            case Opcode.Sigil.HASH:
                return "GetHash";
            case Opcode.Sigil.STASH:
                return "GetStash";
            case Opcode.Sigil.SUB:
                return "GetCode";
            case Opcode.Sigil.GLOB:
                return "GetGlob";
            case Opcode.Sigil.HANDLE:
                return "GetHandle";
            default:
                throw new System.Exception(string.Format("Unhandled slot {0:D}", slot));
            }
        }

        private string PropertyForSlot(Opcode.Sigil slot)
        {
            switch (slot)
            {
            case Opcode.Sigil.SCALAR:
                return "Scalar";
            case Opcode.Sigil.ARRAY:
                return "Array";
            case Opcode.Sigil.HASH:
                return "Hash";
            case Opcode.Sigil.SUB:
                return "Code";
            case Opcode.Sigil.HANDLE:
                return "Handle";
            default:
                throw new System.Exception(string.Format("Unhandled slot {0:D}", slot));
            }
        }

        private ParameterExpression GetTemporary(int index, Type type)
        {
            while (Temporaries.Count <= index)
                Temporaries.Add(null);
            if (Temporaries[index] == null)
            {
                if (type == null)
                    throw new System.Exception("Untyped temporary");
                Temporaries[index] =
                    Expression.Variable(type);
            }

            return Temporaries[index];
        }

        private ParameterExpression GetSavedLexState(int index)
        {
            while (LexStates.Count <= index)
                LexStates.Add(null);
            if (LexStates[index] == null)
            {
                LexStates[index] =
                    Expression.Variable(typeof(SavedLexState));
            }

            return LexStates[index];
        }

        private ParameterExpression GetSavedRxState(int index)
        {
            while (RxStates.Count <= index)
                RxStates.Add(null);
            if (RxStates[index] == null)
            {
                RxStates[index] =
                    Expression.Variable(typeof(RxResult));
            }

            return RxStates[index];
        }

        private Expression GetLexical(int index, Type type)
        {
            while (Lexicals.Count <= index)
                Lexicals.Add(null);
            if (Lexicals[index] == null)
                Lexicals[index] = Expression.Variable(typeof(object));

            return Lexicals[index];
        }

        private Expression GetLexical(int index, Opcode.Sigil slot)
        {
            return GetLexical(index, TypeForSlot(slot));
        }

        private Expression GetLexicalValue(int index, Opcode.Sigil slot)
        {
            return GetLexical(index, slot);
        }

        private Expression GetLexicalPadValue(LexicalInfo info)
        {
            return Expression.Call(
                Pad,
                typeof(P5ScratchPad).GetMethod(MethodForSlot(info.Slot)),
                Runtime,
                Expression.Constant(info.Index));
        }

        private Expression GetLexicalPad(LexicalInfo info)
        {
            return Expression.MakeIndex(
                Pad, Pad.Type.GetProperty("Item"),
                new Expression[] { Expression.Constant(info.Index) });
        }

        private struct ScopeInfo
        {
            public List<Expression> Body;
            public bool Used;
            public BasicBlock FirstBlock;
            public int FirstBlockFor;
            public LabelTarget Start;
        }

        private ScopeInfo[] Scopes;
        private Scope CurrentScope;

        public void GenerateScope(Subroutine sub, Scope scope)
        {
            // TODO should not rely on block order
            BasicBlock first_block = null;
            var exps = new List<Expression>();

            CurrentScope = scope;

            for (int i = 0; i < sub.BasicBlocks.Count; ++i)
            {
                var block = sub.BasicBlocks[i];
                if (block == null || block.Dead != 0) // TODO enumeration
                    continue;

                if (block.Scope > scope.Id)
                {
                    int scopeId = block.Scope;

                    // if scope has no blocks on its own, still needs to
                    // insert inner scopes, so it can't just test whether this
                    // block's scope is directly inside the current scope, but
                    // it must handle the case when a scope is entirely
                    // composed by blocks in inner scopes
                    if (Scopes[scopeId].FirstBlockFor != -1)
                        scopeId = Scopes[scopeId].FirstBlockFor;

                    if (sub.Scopes[scopeId].Outer == scope.Id &&
                        !Scopes[scopeId].Used)
                    {
                        Scopes[scopeId].Used = true;

                        if (Scopes[scopeId].Body != null)
                        {
                            if (first_block != null)
                                exps.Add(Expression.Label(Scopes[scopeId].Start));

                            exps.AddRange(Scopes[scopeId].Body);
                        }

                        if (first_block == null)
                        {
                            first_block = Scopes[block.Scope].FirstBlock;
                            Scopes[block.Scope].FirstBlockFor = scope.Id;
                        }
                    }
                }
                else if (block.Scope == scope.Id)
                {
                    if (first_block == null)
                        first_block = block;
                    else
                        exps.Add(Expression.Label(BlockLabels[block]));

                    GenerateBlock(sub, block, exps);
                }
            }

            List<Expression> body = null;

            if ((scope.Flags & Scope.SCOPE_EVAL) != 0)
            {
                exps.Insert(0,
                    Expression.Call(
                        Expression.Field(Runtime, "CallStack"),
                        typeof(Stack<StackFrame>).GetMethod("Push"),
                        Expression.New(
                            typeof(StackFrame).GetConstructor(new Type[] {
                                    typeof(string), typeof(string),
                                    typeof(int), typeof(P5Code),
                                    typeof(Opcode.ContextValues),
                                    typeof(bool)}),
                            Expression.Constant(sub.LexicalStates[scope.LexicalState].Package),
                            Expression.Constant(scope.Start.File),
                            Expression.Constant(scope.Start.Line),
                            Expression.Constant(null, typeof(P5Code)),
                            Expression.Constant((Opcode.ContextValues)scope.Context),
                            Expression.Constant(true)
                            )));

                var except = new List<Expression>();
                var ex = Expression.Variable(typeof(P5Exception));
                for (int j = scope.Opcodes.Count - 1; j >= 0; --j)
                    GenerateOpcodes(sub, scope.Opcodes[j], except);
                except.Add(
                    Expression.Call(
                        Runtime,
                        typeof(Runtime).GetMethod("SetException"),
                        ex));
                GenerateBlock(sub, scope.Exception, except);

                var block = Expression.TryCatchFinally(
                    Expression.Block(typeof(object), exps),
                    Expression.Call(
                        Expression.Field(Runtime, "CallStack"),
                        typeof(Stack<StackFrame>).GetMethod("Pop")),
                    Expression.Catch(
                        ex,
                        Expression.Block(typeof(object), except))
                    );

                body = new List<Expression>();
                body.Add(block);
            }
            else if (scope.Opcodes.Count > 0)
            {
                var fault = new List<Expression>();
                for (int j = scope.Opcodes.Count - 1; j >= 0; --j)
                    GenerateOpcodes(sub, scope.Opcodes[j], fault);
                fault.Add(Expression.Rethrow(typeof(object)));

                var block = Expression.TryCatch(
                    Expression.Block(typeof(object), exps),
                    Expression.Catch(
                        typeof(System.Exception),
                        Expression.Block(typeof(object), fault)));

                body = new List<Expression>();
                body.Add(block);
            }
            else if (first_block != null)
                body = exps;

            if (first_block != null)
                Scopes[scope.Id].Start = BlockLabels[first_block];

            Scopes[scope.Id].FirstBlock = first_block;

            if ((scope.Flags & Scope.SCOPE_VALUE) != 0)
                ValueBlocks[first_block] = Expression.Block(
                    typeof(object), body);
            else
                Scopes[scope.Id].Body = body;
        }

        private Expression GenerateScopes(Subroutine sub)
        {
            Scopes = new ScopeInfo[sub.Scopes.Count];

            for (int i = 0; i < Scopes.Length; ++i)
                Scopes[i].FirstBlockFor = -1;

            for (int i = sub.Scopes.Count - 1; i >= 0; --i)
                GenerateScope(sub, sub.Scopes[i]);

            return Expression.Block(typeof(object), Scopes[0].Body);
        }

        public LambdaExpression Generate(Subroutine sub, bool is_main)
        {
            IsMain = is_main;
            SubLabel = Expression.Label(typeof(object));

            for (int i = 0; i < sub.BasicBlocks.Count; ++i)
                if (sub.BasicBlocks[i] != null)
                    BlockLabels[sub.BasicBlocks[i]] = Expression.Label("L" + i.ToString());

            var scopes = GenerateScopes(sub);

            var vars = new List<ParameterExpression>();
            AddVars(vars, Variables);
            AddVars(vars, Lexicals);
            AddVars(vars, Temporaries);
            AddVars(vars, LexStates);
            AddVars(vars, RxStates);

            var block = Expression.Block(typeof(object), vars, scopes);
            var args = new ParameterExpression[] { Runtime, Context, Pad, Arguments };
            return Expression.Lambda<P5Code.Sub>(Expression.Label(SubLabel, block), args);
        }

        private void AddVars(List<ParameterExpression> vars,
                             List<ParameterExpression> toAdd)
        {
            foreach (var i in toAdd)
                if (i != null)
                    vars.Add(i);
        }

        public void UpdateFileLine(Opcode op, List<Expression> expressions)
        {
            expressions.Add(
                Expression.Assign(
                    Expression.Field(Runtime, "File"),
                    Expression.Constant(op.Position.File)));
            expressions.Add(
                Expression.Assign(
                    Expression.Field(Runtime, "Line"),
                    Expression.Constant(op.Position.Line)));
/*
            expressions.Add(
                Expression.Call(
                    typeof(Builtins).GetMethod("TracePosition"),
                    Expression.Constant(op.Position.File),
                    Expression.Constant(op.Position.Line)));
*/
        }

        public void GenerateBlock(Subroutine sub, BasicBlock bb,
                                  List<Expression> expressions)
        {
            foreach (var o in bb.Opcodes)
            {
                if (o.Position.File != null)
                    UpdateFileLine(o, expressions);
                expressions.Add(Generate(sub, o));
            }
        }

        public void GenerateOpcodes(Subroutine sub, IList<Opcode> ops,
                                   List<Expression> expressions)
        {
            foreach (var o in ops)
            {
                if (o.Position.File != null)
                    UpdateFileLine(o, expressions);
                expressions.Add(Generate(sub, o));
            }
        }

        public Expression GenerateJump(Subroutine sub, Opcode op,
                                       ExpressionType type, bool is_string)
        {
            var ju = (CondJump)op;

            Expression cmp;

            if (is_string)
                cmp = StringRelOperator(sub, op, type);
            else
                cmp = NumericRelOperator(sub, op, type);
            Expression jump = Expression.Goto(
                BlockLabels[ju.To],
                typeof(object));

            return Expression.IfThen(
                Expression.Convert(cmp, typeof(bool)), jump);
        }

        public Expression GenerateBooleanJump(Subroutine sub, Opcode op)
        {
            var ju = (CondJump)op;

            Expression cmp = ConvertBoolean(sub, op);
            Expression jump = Expression.Goto(
                BlockLabels[ju.To],
                typeof(object));

            return Expression.IfThen(
                Expression.Convert(cmp, typeof(bool)), jump);
        }

        private Expression ReturnExpression(Expression value)
        {
            return Expression.Return(
                SubLabel,
                Expression.Call(
                    typeof(Builtins).GetMethod("Return"),
                    Runtime,
                    Context,
                    value),
                typeof(object));
        }

        private Expression MakeFlatArray(Subroutine sub, Opcode op)
        {
            var data = new List<Expression>();
            var temp = Expression.Variable(typeof(List<object>));
            var method = typeof(Builtins).GetMethod("PushFlattenIListObject");

            data.Add(
                Expression.Assign(
                    temp,
                    Expression.New(
                        typeof(List<object>).GetConstructor(ProtoInt),
                        Expression.Constant(op.Childs.Length))));

            foreach (var i in op.Childs)
                data.Add(
                    Expression.Call(method, Runtime, temp, Generate(sub, i)));

            data.Add(temp);

            return Expression.Block(
                new ParameterExpression[] { temp }, data);
        }

        private Expression MakeNonFlatArray(Subroutine sub, Opcode op)
        {
            throw new System.Exception("Should not be used");
/*
            var data = new List<Expression>();
            var temp = Expression.Variable(typeof(List<IP5Any>));
            var method = typeof(List<IP5Any>).GetMethod("Add");

            data.Add(
                Expression.Assign(
                    temp,
                    Expression.New(
                        typeof(List<IP5Any>).GetConstructor(ProtoInt),
                        Expression.Constant(op.Childs.Length))));

            foreach (var i in op.Childs)
                data.Add(
                    Expression.Call(temp, method, Generate(sub, i)));

            data.Add(
                Expression.New(
                    typeof(P5LvalueList).GetConstructor(
                        new Type[] { typeof(Runtime), typeof(List<IP5Any>) }),
                    Runtime,
                    temp));

            return Expression.Block(
                new ParameterExpression[] { temp }, data);
*/
        }

        private Expression ScalarAssignmentExpression(Subroutine sub, Opcode.ContextValues cxt, Opcode lvalue, Opcode rvalue)
        {
            return ScalarAssignmentExpression(sub, cxt, lvalue,
                                              Generate(sub, rvalue));
        }

        private Expression ScalarAssignmentExpression(Subroutine sub, Opcode.ContextValues cxt, Opcode lvalue, Expression rvalue)
        {
            switch (lvalue.Number)
            {
            case Opcode.OpNumber.OP_ARRAY_ELEMENT:
                return ArrayItemAssign(sub, cxt, Generate(sub, lvalue.Childs[0]),
                                       Generate(sub, lvalue.Childs[1]),
                                       rvalue);
            case Opcode.OpNumber.OP_HASH_ELEMENT:
                return HashItemAssign(sub, cxt, Generate(sub, lvalue.Childs[0]),
                                      Generate(sub, lvalue.Childs[1]),
                                      rvalue);
            case Opcode.OpNumber.OP_LEXICAL:
            {
                var lvalue_exp = Generate(sub, lvalue);

                return Expression.Assign(
                    lvalue_exp, ScalarAssign(sub, cxt, lvalue_exp,
                                             rvalue));
            }
            default:
                return ScalarAssign(sub, cxt, Generate(sub, lvalue),
                                    rvalue);
            }
        }

        // TODO move to opcode

        private static bool IsArray(Opcode.Sigil sigil)
        {
            return sigil == Opcode.Sigil.ARRAY;
        }

        private static bool IsHash(Opcode.Sigil sigil)
        {
            return sigil == Opcode.Sigil.HASH;
        }

        private static bool IsList(Opcode.Sigil sigil)
        {
            return sigil == Opcode.Sigil.ARRAY || sigil == Opcode.Sigil.HASH;
        }

        private static bool IsList(Opcode op)
        {
            switch (op.Number)
            {
            case Opcode.OpNumber.OP_LEXICAL:
            case Opcode.OpNumber.OP_LEXICAL_PAD:
                return IsList(((Lexical)op).Slot);
            case Opcode.OpNumber.OP_GLOBAL:
                return IsList(((Global)op).Slot);
            case Opcode.OpNumber.OP_GLOB_SLOT:
                return IsList(((GlobSlot)op).Slot);
            default:
                // TODO missing cases
                return false;
            }
        }

        private static bool IsHash(Opcode op)
        {
            switch (op.Number)
            {
            case Opcode.OpNumber.OP_LEXICAL:
            case Opcode.OpNumber.OP_LEXICAL_PAD:
                return IsHash(((Lexical)op).Slot);
            case Opcode.OpNumber.OP_GLOBAL:
                return IsHash(((Global)op).Slot);
            case Opcode.OpNumber.OP_GLOB_SLOT:
                return IsHash(((GlobSlot)op).Slot);
            default:
                // TODO missing cases
                return false;
            }
        }

        private static bool IsScalar(Opcode op)
        {
            return !IsList(op);
        }

        private Expression ArrayAssignmentExpression(Subroutine sub, Opcode.ContextValues cxt, Opcode lvalue, Opcode rvalue, bool common)
        {
            var exps = new List<Expression>();
            var src = Expression.Variable(typeof(object));
            var rvalue_exp = Generate(sub, rvalue);
            var lvalue_exp = Expression.Variable(typeof(List<object>));

            if (common)
            {
                exps.Add(
                    Expression.Assign(
                        src,
                        Expression.Call(
                            typeof(Builtins).GetMethod("CloneObject"),
                            Runtime,
                            rvalue_exp,
                            Expression.Constant(1))));
            }
            else
                exps.Add(
                    Expression.Assign(
                        src,
                        rvalue_exp));

            Opcode[] lvalues = null;
            bool single_lvalue;
            bool list_context =
                cxt == Opcode.ContextValues.CALLER ||
                cxt == Opcode.ContextValues.LIST;

            if (list_context)
                exps.Add(
                    Expression.Assign(
                        lvalue_exp,
                        Expression.New(
                            typeof(List<object>).GetConstructor(new Type[0]))));

            if (lvalue.Number == Opcode.OpNumber.OP_MAKE_LIST)
            {
                lvalues = lvalue.Childs;
                single_lvalue = false;
            }
            else
            {
                lvalues = new Opcode[] { lvalue };
                single_lvalue = true;
            }

            var count = Expression.Variable(typeof(int));
            var iter = Expression.Variable(typeof(IEnumerator));

            exps.Add(
                Expression.Call(
                    typeof(Builtins).GetMethod("PrepareArrayAssignment"),
                    Runtime,
                    src, iter, count));

            var next = Expression.Condition(
                Expression.Call(
                    iter,
                    typeof(IEnumerator).GetMethod("MoveNext")),
                Expression.Property(iter, "Current"),
                Expression.Constant(null, typeof(object)));
            var method = typeof(Builtins).GetMethod("PushFlattenIListObject");

            foreach (var e in lvalues)
            {
                if (IsScalar(e))
                {
                    var item_assign =
                        ScalarAssignmentExpression(sub, cxt, e, next);

                    if (list_context)
                        exps.Add(
                            Expression.Call(
                                method, Runtime, lvalue_exp, item_assign));
                    else
                        exps.Add(item_assign);
                }
                else
                {
                    var assign_method = IsHash(e) ? "AssignHashIterator" : "AssignArrayIterator";
                    var item_exp = Generate(sub, e);
                    var item_assign = Expression.Call(
                        typeof(Builtins).GetMethod(assign_method),
                        Runtime, item_exp, iter);
                    Expression exp = item_assign;

                    switch (e.Number)
                    {
                    case Opcode.OpNumber.OP_LEXICAL:
                        // handles assignment to undefined lexical
                        exp = Expression.Assign(item_exp, exp);
                        break;
                    default:
                        // nothing to do
                        break;
                    }

                    if (single_lvalue && list_context)
                        // TODO optimize and avoid copy
                        exp = Expression.Call(
                            method, Runtime, lvalue_exp, exp);
                    else if (list_context)
                        exp = Expression.Call(
                            method, Runtime, lvalue_exp, exp);

                    exps.Add(exp);
                }
            }

            if (cxt == Opcode.ContextValues.CALLER)
                exps.Add(
                    Expression.Condition(
                        Expression.Equal(
                            Context,
                            Expression.Constant(Opcode.ContextValues.SCALAR)),
                        Expression.Convert(count, typeof(object)),
                        lvalue_exp,
                        typeof(object)));
            else if (cxt == Opcode.ContextValues.SCALAR)
                exps.Add(Expression.Convert(count, typeof(object)));
            else
                exps.Add(lvalue_exp);

            var parms = new ParameterExpression[] {
                iter, count, src, lvalue_exp };

            return Expression.Block(
                typeof(object), parms, exps);
        }

        protected abstract Expression ConstantInteger(int value);
        protected abstract Expression ConstantFloat(double value);
        protected abstract Expression ConstantSub(Subroutine sub);
        protected abstract Expression ConstantRegex(Subroutine sub);

        protected abstract Expression Builtin(Subroutine sub, Opcode op, string prefix, int count, params Expression[] extra);
        protected abstract Expression UnaryOperator(Subroutine sub, Opcode op, ExpressionType operation);
        protected abstract Expression UnaryIncrement(Subroutine sub, Opcode op, ExpressionType operation);
        protected abstract Expression BinaryOperator(Subroutine sub, Opcode op, ExpressionType operation);
        protected abstract Expression StringOperator(Subroutine sub, Opcode op, ExpressionType operation);
        protected abstract Expression ConvertBoolean(Subroutine sub, Opcode op);
        protected abstract Expression NumericRelOperator(Subroutine sub, Opcode op, ExpressionType operation);
        protected abstract Expression StringRelOperator(Subroutine sub, Opcode op, ExpressionType operation);
        protected abstract Expression ScalarAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue);
        protected abstract Expression ArrayItem(Subroutine sub, Opcode.ContextValues cxt, Expression value, Expression index, bool create);
        protected abstract Expression ArrayItemAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression index, Expression rvalue);
        protected abstract Expression HashItem(Subroutine sub, Opcode.ContextValues cxt, Expression value, Expression index, bool create);
        protected abstract Expression HashItemAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression index, Expression rvalue);
        protected abstract Expression Iterator(Subroutine sub, Expression value);
        protected abstract Expression Defined(Subroutine sub, Opcode op);
        protected abstract void DefinePackage(string pack);
        protected abstract Expression AccessGlobal(Expression runtime_exp, Opcode.Sigil slot, string name, bool create);

        public Expression Generate(Subroutine sub, Opcode op)
        {
            switch(op.Number)
            {
            case Opcode.OpNumber.OP_FRESH_STRING:
            case Opcode.OpNumber.OP_CONSTANT_STRING:
            {
                var cs = (ConstantString)op;

                return
                    Expression.New(
                        typeof(P5Scalar).GetConstructor(ProtoRuntimeString),
                        new Expression[] {
                            Runtime,
                            Expression.Constant(cs.Value) });
            }
            case Opcode.OpNumber.OP_CONSTANT_UNDEF:
            {
                var ctor = typeof(P5Scalar).GetConstructor(ProtoRuntime);

                return Expression.New(ctor, new Expression[] { Runtime });
            }
            case Opcode.OpNumber.OP_CONSTANT_INTEGER:
                return ConstantInteger(((ConstantInt)op).Value);
            case Opcode.OpNumber.OP_CONSTANT_FLOAT:
                return ConstantFloat(((ConstantFloat)op).Value);
            case Opcode.OpNumber.OP_CONSTANT_SUB:
            {
                var cs = (ConstantSub)op;

                return ConstantSub(cs.Value);
            }
            case Opcode.OpNumber.OP_UNDEF:
            {
                return Expression.Call(
                    Generate(sub, op.Childs[0]),
                    typeof(IP5Any).GetMethod("Undef"),
                    Runtime);
            }
            case Opcode.OpNumber.OP_GLOBAL:
            {
                Global gop = (Global)op;
                bool create;

                if (gop.Slot == Opcode.Sigil.STASH)
                    create = (gop.Context & (int)Opcode.ContextValues.NOCREATE) == 0;
                else
                    create = true;

                return AccessGlobal(Runtime, gop.Slot, gop.Name, create);
            }
            case Opcode.OpNumber.OP_GLOB_SLOT:
            {
                GlobSlot gop = (GlobSlot)op;
                string name = PropertyForSlot(gop.Slot);

                return Expression.Property(
                    Utils.CheckedConvert(
                        Generate(sub, op.Childs[0]),
                        typeof(P5Typeglob)),
                    name);
            }
            case Opcode.OpNumber.OP_SWAP_GLOB_SLOT_SET:
            {
                GlobSlot gop = (GlobSlot)op;
                string name = PropertyForSlot(gop.Slot);
                var property = Expression.Property(
                    Utils.CheckedConvert(
                        Generate(sub, op.Childs[1]),
                        typeof(P5Typeglob)),
                    name);

                // TODO change properties to be just objects
                return Expression.Assign(
                    property,
                    Expression.Call(
                        typeof(Builtins).GetMethod("UpgradeScalar"),
                        Runtime,
                        Generate(sub, op.Childs[0])));
            }
            case Opcode.OpNumber.OP_MAKE_LIST:
            {
                if ((op.Context & (int)Opcode.ContextValues.LVALUE) != 0)
                    return MakeNonFlatArray(sub, op);
                else
                    return MakeFlatArray(sub, op);
            }
            case Opcode.OpNumber.OP_MAKE_ARRAY:
                return MakeFlatArray(sub, op);
            case Opcode.OpNumber.OP_DOT_DOT:
                // TODO needs to handle the flip/flop mode in scalar context
                return Builtin(sub, op, "MakeRange", 0);
            case Opcode.OpNumber.OP_ANONYMOUS_ARRAY:
                return Expression.Call(
                    typeof(Builtins).GetMethod("AnonymousArray"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_ANONYMOUS_HASH:
                return Expression.Call(
                    typeof(Builtins).GetMethod("AnonymousHash"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_PRINT:
                return Expression.Call(
                    typeof(Builtins).GetMethod("Print"),
                    Runtime,
                    Generate(sub, op.Childs[0]),
                    Generate(sub, op.Childs[1]));
            case Opcode.OpNumber.OP_READLINE:
            {
                return
                    Expression.Call(
                        typeof(Builtins).GetMethod("Readline"),
                        Runtime,
                        Expression.Call(
                            Generate(sub, op.Childs[0]),
                            typeof(IP5Any).GetMethod("DereferenceHandle"),
                            Runtime),
                        OpContext(op));
            }
            case Opcode.OpNumber.OP_END:
            {
                return
                    Expression.Return(
                        SubLabel,
                        Expression.Constant(null, typeof(object)),
                        typeof(object));
            }
            case Opcode.OpNumber.OP_STOP:
            {
                // TODO remove STOP
                return Generate(sub, op.Childs[0]);
            }
            case Opcode.OpNumber.OP_DIE:
            {
                return Expression.Block(
                    Expression.Throw(
                        Expression.Call(
                            typeof(Builtins).GetMethod("Die"),
                            Runtime,
                            Generate(sub, op.Childs[0]))),
                    // this is only to trick the type checker into
                    // thinking that this is a "normal" expression
                    Expression.Constant(null, typeof(object)));
            }
            case Opcode.OpNumber.OP_WARN:
            {
                return Expression.Call(
                    typeof(Builtins).GetMethod("Warn"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_SPRINTF:
            {
                return Expression.Call(
                    typeof(Builtins).GetMethod("Sprintf"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_DO_FILE:
            {
                return
                    Expression.Call(
                        typeof(Builtins).GetMethod("DoFile"),
                        Runtime,
                        OpContext(op),
                        Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_REQUIRE_FILE:
            {
                return
                    Expression.Call(
                        typeof(Builtins).GetMethod("RequireFile"),
                        Runtime,
                        OpContext(op),
                        Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_WANTARRAY:
            {
                return
                    Expression.Call(
                        typeof(Builtins).GetMethod("WantArray"),
                        Runtime,
                        Context);
            }
            case Opcode.OpNumber.OP_SCALAR:
            {
                var child = op.Childs[0];

                if (child.Number == Opcode.OpNumber.OP_MAKE_LIST)
                {
                    var count = child.Childs.Length;
                    var exps = new List<Expression>(count);

                    for (int i = 0; i < count - 1; ++i)
                        exps.Add(Generate(sub, child.Childs[i]));

                    exps.Add(
                        Expression.Call(
                            typeof(Builtins).GetMethod("ConvertToScalarValue"),
                            Runtime,
                            Generate(sub, child.Childs[count - 1])));

                    return Expression.Block(exps);
                }

                return Expression.Call(
                    typeof(Builtins).GetMethod("ConvertToScalarValue"),
                    Runtime,
                    Generate(sub, child));
            }
            case Opcode.OpNumber.OP_RETURN:
                return ReturnExpression(Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_DYNAMIC_GOTO:
            {
                // TODO handle goto $LABEL
                var exit_scope = new List<Expression>();
                var value = Expression.Parameter(typeof(P5Code));
                var code = Utils.CheckedConvert(
                    Builtin(sub, op, "DereferenceSubroutine", 0),
                    typeof(P5Code));

                exit_scope.Add(
                    Expression.Assign(value, code));

                for (var s = CurrentScope; s != null; s = s.Outer != -1 ? sub.Scopes[s.Outer] : null)
                    for (int j = s.Opcodes.Count - 1; j >= 0; --j)
                        GenerateOpcodes(sub, s.Opcodes[j], exit_scope);

                exit_scope.Add(
                    Expression.Call(
                        Expression.Field(Runtime, "CallStack"),
                        typeof(Stack<StackFrame>).GetMethod("Pop")));

                // TODO this is not a real tail call: the .Net stack grows
                exit_scope.Add(
                    ReturnExpression(
                        Expression.Call(
                            value,
                            typeof(P5Code).GetMethod("Call"),
                            Runtime,
                            Context,
                            Arguments)));

                return Expression.Block(typeof(object), new[] { value }, exit_scope);
            }
            case Opcode.OpNumber.OP_ASSIGN_LIST:
            {
                bool common = ((ListAssign)op).Common != 0;

                return ArrayAssignmentExpression(
                    sub, (Opcode.ContextValues)op.Context,
                    op.Childs[1], op.Childs[0], common);
            }
            case Opcode.OpNumber.OP_SWAP_ASSIGN_LIST:
            {
                bool common = ((ListAssign)op).Common != 0;

                return ArrayAssignmentExpression(
                    sub, (Opcode.ContextValues)op.Context,
                    op.Childs[0], op.Childs[1], common);
            }
            case Opcode.OpNumber.OP_ASSIGN:
                return ScalarAssignmentExpression(
                    sub, (Opcode.ContextValues)op.Context, op.Childs[1],
                    op.Childs[0]);
            case Opcode.OpNumber.OP_SWAP_ASSIGN:
                return ScalarAssignmentExpression(
                    sub, (Opcode.ContextValues)op.Context, op.Childs[0],
                    op.Childs[1]);
            case Opcode.OpNumber.OP_GET:
            {
                GetSet gs = (GetSet)op;

                return GetVariable(gs.Index, typeof(object));
            }
            case Opcode.OpNumber.OP_SET:
            {
                GetSet gs = (GetSet)op;
                var e = Generate(sub, op.Childs[0]);

                return Expression.Assign(
                    GetVariable(gs.Index, typeof(object)),
                    e);
            }
            case Opcode.OpNumber.OP_JUMP:
                return Expression.Goto(BlockLabels[((Jump)op).To], typeof(object));
            case Opcode.OpNumber.OP_JUMP_IF_NULL:
            {
                Expression cmp = Expression.Equal(
                    Generate(sub, op.Childs[0]),
                    Expression.Constant(null, typeof(object)));
                Expression jump = Expression.Goto(
                    BlockLabels[((CondJump)op).To],
                    typeof(object));

                return Expression.IfThen(cmp, jump);
            }
            case Opcode.OpNumber.OP_JUMP_IF_S_EQ:
                return GenerateJump(sub, op, ExpressionType.Equal, true);
            case Opcode.OpNumber.OP_JUMP_IF_S_NE:
                return GenerateJump(sub, op, ExpressionType.NotEqual, true);
            case Opcode.OpNumber.OP_JUMP_IF_F_EQ:
                return GenerateJump(sub, op, ExpressionType.Equal, false);
            case Opcode.OpNumber.OP_JUMP_IF_F_NE:
                return GenerateJump(sub, op, ExpressionType.NotEqual, false);
            case Opcode.OpNumber.OP_JUMP_IF_F_GE:
                return GenerateJump(sub, op, ExpressionType.GreaterThanOrEqual, false);
            case Opcode.OpNumber.OP_JUMP_IF_F_LE:
                return GenerateJump(sub, op, ExpressionType.LessThanOrEqual, false);
            case Opcode.OpNumber.OP_JUMP_IF_F_GT:
                return GenerateJump(sub, op, ExpressionType.GreaterThan, false);
            case Opcode.OpNumber.OP_JUMP_IF_F_LT:
                return GenerateJump(sub, op, ExpressionType.LessThan, false);
            case Opcode.OpNumber.OP_JUMP_IF_TRUE:
                return GenerateBooleanJump(sub, op);
            case Opcode.OpNumber.OP_LOG_NOT:
                return UnaryOperator(sub, op, ExpressionType.Not);
            case Opcode.OpNumber.OP_MINUS:
                return UnaryOperator(sub, op, ExpressionType.Negate);
            case Opcode.OpNumber.OP_DEFINED:
                return Defined(sub, op);
            case Opcode.OpNumber.OP_ORD:
                return Builtin(sub, op, "Ord", 1);
            case Opcode.OpNumber.OP_CHR:
                return Builtin(sub, op, "Chr", 1);
            case Opcode.OpNumber.OP_UC:
                return Builtin(sub, op, "Uppercase", 1);
            case Opcode.OpNumber.OP_LC:
                return Builtin(sub, op, "Lowercase", 1);
            case Opcode.OpNumber.OP_OCT:
                return Expression.Call(
                    typeof(Builtins).GetMethod("Oct"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_HEX:
                return Expression.Call(
                    typeof(Builtins).GetMethod("Hex"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_INDEX:
            {
                Expression start;

                if (op.Childs.Length == 3)
                    start = Expression.Call(
                        typeof(Builtins).GetMethod("ConvertToInteger"),
                        Runtime,
                        Generate(sub, op.Childs[2]));
                else
                    start = Expression.Constant(0);

                return Expression.Call(
                    typeof(Builtins).GetMethod("Index"),
                    Runtime,
                    Generate(sub, op.Childs[0]),
                    Generate(sub, op.Childs[1]),
                    start);
            }
            case Opcode.OpNumber.OP_CONCATENATE:
                return StringOperator(sub, op, ExpressionType.Add);
            case Opcode.OpNumber.OP_CONCATENATE_ASSIGN:
                return StringOperator(sub, op, ExpressionType.AddAssign);
            case Opcode.OpNumber.OP_ARRAY_LENGTH:
            {
                Expression len =
                    Expression.Call(
                        Utils.CheckedConvert(
                            Generate(sub, op.Childs[0]),
                            typeof(IP5Array)),
                        typeof(IP5Array).GetMethod("GetCount"),
                        Runtime);
                Expression len_1 = Expression.Subtract(len, Expression.Constant(1));
                return Expression.New(
                    typeof(P5Scalar).GetConstructor(ProtoRuntimeInt),
                    new Expression[] { Runtime, len_1 });
            }
            case Opcode.OpNumber.OP_BIT_NOT:
                return UnaryOperator(sub, op, ExpressionType.OnesComplement);
            case Opcode.OpNumber.OP_BIT_OR:
                return BinaryOperator(sub, op, ExpressionType.Or);
            case Opcode.OpNumber.OP_BIT_OR_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.OrAssign);
            case Opcode.OpNumber.OP_BIT_AND:
                return BinaryOperator(sub, op, ExpressionType.And);
            case Opcode.OpNumber.OP_BIT_AND_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.AndAssign);
            case Opcode.OpNumber.OP_BIT_XOR:
                return BinaryOperator(sub, op, ExpressionType.ExclusiveOr);
            case Opcode.OpNumber.OP_BIT_XOR_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.ExclusiveOrAssign);
            case Opcode.OpNumber.OP_NUM_LE:
                return NumericRelOperator(sub, op, ExpressionType.LessThanOrEqual);
            case Opcode.OpNumber.OP_NUM_LT:
                return NumericRelOperator(sub, op, ExpressionType.LessThan);
            case Opcode.OpNumber.OP_NUM_EQ:
                return NumericRelOperator(sub, op, ExpressionType.Equal);
            case Opcode.OpNumber.OP_NUM_NE:
                return NumericRelOperator(sub, op, ExpressionType.NotEqual);
            case Opcode.OpNumber.OP_NUM_GE:
                return NumericRelOperator(sub, op, ExpressionType.GreaterThanOrEqual);
            case Opcode.OpNumber.OP_NUM_GT:
                return NumericRelOperator(sub, op, ExpressionType.GreaterThan);
            case Opcode.OpNumber.OP_STR_LE:
                return StringRelOperator(sub, op, ExpressionType.LessThanOrEqual);
            case Opcode.OpNumber.OP_STR_LT:
                return StringRelOperator(sub, op, ExpressionType.LessThan);
            case Opcode.OpNumber.OP_STR_EQ:
                return StringRelOperator(sub, op, ExpressionType.Equal);
            case Opcode.OpNumber.OP_STR_NE:
                return StringRelOperator(sub, op, ExpressionType.NotEqual);
            case Opcode.OpNumber.OP_STR_GE:
                return StringRelOperator(sub, op, ExpressionType.GreaterThanOrEqual);
            case Opcode.OpNumber.OP_STR_GT:
                return StringRelOperator(sub, op, ExpressionType.GreaterThan);
            case Opcode.OpNumber.OP_ADD:
                return BinaryOperator(sub, op, ExpressionType.Add);
            case Opcode.OpNumber.OP_ADD_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.AddAssign);
            case Opcode.OpNumber.OP_SUBTRACT:
                return BinaryOperator(sub, op, ExpressionType.Subtract);
            case Opcode.OpNumber.OP_SUBTRACT_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.SubtractAssign);
            case Opcode.OpNumber.OP_MULTIPLY:
                return BinaryOperator(sub, op, ExpressionType.Multiply);
            case Opcode.OpNumber.OP_MULTIPLY_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.MultiplyAssign);
            case Opcode.OpNumber.OP_DIVIDE:
                return BinaryOperator(sub, op, ExpressionType.Divide);
            case Opcode.OpNumber.OP_DIVIDE_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.DivideAssign);
            case Opcode.OpNumber.OP_SHIFT_LEFT:
                return BinaryOperator(sub, op, ExpressionType.LeftShift);
            case Opcode.OpNumber.OP_SHIFT_LEFT_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.LeftShiftAssign);
            case Opcode.OpNumber.OP_SHIFT_RIGHT:
                return BinaryOperator(sub, op, ExpressionType.RightShift);
            case Opcode.OpNumber.OP_SHIFT_RIGHT_ASSIGN:
                return BinaryOperator(sub, op, ExpressionType.RightShiftAssign);
            case Opcode.OpNumber.OP_PREINC:
                return UnaryIncrement(sub, op, ExpressionType.PreIncrementAssign);
            case Opcode.OpNumber.OP_PREDEC:
                return UnaryIncrement(sub, op, ExpressionType.PreDecrementAssign);
            case Opcode.OpNumber.OP_POSTINC:
                return UnaryIncrement(sub, op, ExpressionType.PostIncrementAssign);
            case Opcode.OpNumber.OP_POSTDEC:
                return UnaryIncrement(sub, op, ExpressionType.PostDecrementAssign);
            case Opcode.OpNumber.OP_REVERSE:
                return Expression.Call(
                    typeof(Builtins).GetMethod("Reverse"),
                    Runtime,
                    OpContext(op),
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_REPEAT_ARRAY:
                // TODO overload
                return Builtin(sub, op, "RepeatArray", 0);
            case Opcode.OpNumber.OP_REPEAT_SCALAR:
                // TODO overload
                return Builtin(sub, op, "RepeatScalar", 0);
            case Opcode.OpNumber.OP_SORT:
                return Builtin(sub, op, "Sort", 0);
            case Opcode.OpNumber.OP_ARRAY_ELEMENT:
            {
                var ea = (ElementAccess)op;

                return ArrayItem(sub, (Opcode.ContextValues)op.Context,
                                 Generate(sub, op.Childs[0]),
                                 Generate(sub, op.Childs[1]),
                                 ea.Create == 1);
            }
            case Opcode.OpNumber.OP_HASH_ELEMENT:
            {
                var ea = (ElementAccess)op;

                return HashItem(sub, (Opcode.ContextValues)op.Context,
                                Generate(sub, op.Childs[0]),
                                Generate(sub, op.Childs[1]),
                                ea.Create == 1);
            }
            case Opcode.OpNumber.OP_DELETE_HASH:
            {
                return Expression.Call(
                    Generate(sub, op.Childs[0]),
                    typeof(P5Hash).GetMethod("Delete"),
                    Runtime,
                    Generate(sub, op.Childs[1]));
            }
            case Opcode.OpNumber.OP_EXISTS_ARRAY:
            {
                return Expression.Call(
                    Generate(sub, op.Childs[0]),
                    typeof(P5Array).GetMethod("Exists"),
                    Runtime,
                    Generate(sub, op.Childs[1]));
            }
            case Opcode.OpNumber.OP_EXISTS_HASH:
            {
                return Expression.Convert(
                    Expression.Call(
                        Utils.CheckedConvert(
                            Generate(sub, op.Childs[0]),
                            typeof(P5Hash)),
                        typeof(P5Hash).GetMethod("ExistsKey"),
                        Runtime,
                        Expression.Call(
                            typeof(Builtins).GetMethod("ConvertToKeyString"),
                            Runtime,
                            Generate(sub, op.Childs[1]))),
                    typeof(object));
            }
            case Opcode.OpNumber.OP_PUSH_ELEMENT:
                return Expression.Call(
                    typeof(Builtins).GetMethod("PushListIListObject"),
                    Runtime,
                    Expression.Convert(
                        Generate(sub, op.Childs[0]),
                        typeof(List<object>)),
                    Generate(sub, op.Childs[1]));
            case Opcode.OpNumber.OP_ARRAY_PUSH:
                return Builtin(sub, op, "PushList", 2);
            case Opcode.OpNumber.OP_ARRAY_UNSHIFT:
                return Builtin(sub, op, "UnshiftList", 2);
            case Opcode.OpNumber.OP_ARRAY_POP:
                return Builtin(sub, op, "PopElement", 1);
            case Opcode.OpNumber.OP_ARRAY_SHIFT:
                return Builtin(sub, op, "ShiftElement", 1);
            case Opcode.OpNumber.OP_QUOTEMETA:
                return Builtin(sub, op, "QuoteMeta", 1);
            case Opcode.OpNumber.OP_STRINGIFY:
                // TODO use Builtin() binder
                return Expression.Call(
                    typeof(Builtins).GetMethod("ConvertToString"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_LENGTH:
                return Builtin(sub, op, "StringLength", 1);
            case Opcode.OpNumber.OP_JOIN:
                return Builtin(sub, op, "Join", 1);
            case Opcode.OpNumber.OP_ITERATOR:
                return Iterator(sub, Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_ITERATOR_NEXT:
            {
                Expression iter = Generate(sub, op.Childs[0]);
                Expression has_next =
                    Expression.Call(
                        iter, typeof(IEnumerator).GetMethod("MoveNext"));

                return Expression.Condition(
                    has_next,
                    Expression.Property(iter, "Current"),
                    Expression.Constant(null, typeof(object)));
            }
            case Opcode.OpNumber.OP_SPLICE:
            {
                var args = new Expression[
                    op.Childs.Length > 3 ? 4 :
                    op.Childs.Length < 3 ? 2 : 3];

                args[0] = Generate(sub, op.Childs[0]);

                if (op.Childs.Length > 1)
                    args[1] = Generate(sub, op.Childs[1]);
                else
                    args[1] = Expression.Constant(0);

                if (op.Childs.Length > 2)
                    args[2] = Generate(sub, op.Childs[2]);

                if (op.Childs.Length > 3)
                {
                    var list = new Expression[op.Childs.Length - 3];

                    for (int i = 3; i < op.Childs.Length; ++i)
                        list[i - 3] = Generate(sub, op.Childs[i]);

                    args[3] = Expression.NewArrayInit(typeof(object), list);
                }

                if (op.Childs.Length == 3)
                    return Builtin(sub, NullOpcode, "SpliceCount", 0, args);
                else if (op.Childs.Length < 3)
                    return Builtin(sub, NullOpcode, "SpliceAll", 0, args);
                else
                    return Builtin(sub, NullOpcode, "Replace", 0, args);
            }
            case Opcode.OpNumber.OP_ARRAY_SLICE:
            {
                var ea = (ElementAccess)op;

                return Builtin(sub, op, "SliceArray", 2,
                               Expression.Constant(ea.Create != 0));
            }
            case Opcode.OpNumber.OP_HASH_SLICE:
            {
                var ea = (ElementAccess)op;

                return Builtin(sub, op, "SliceArray", 2,
                               Expression.Constant(ea.Create != 0));
            }
            case Opcode.OpNumber.OP_LIST_SLICE:
                return Builtin(sub, op, "SliceList", 2);
            case Opcode.OpNumber.OP_KEYS:
                return Expression.Call(
                    Generate(sub, op.Childs[0]),
                    typeof(P5Hash).GetMethod("Keys"),
                    Runtime);
            case Opcode.OpNumber.OP_VALUES:
                return Expression.Call(
                    Generate(sub, op.Childs[0]),
                    typeof(P5Hash).GetMethod("Values"),
                    Runtime);
            case Opcode.OpNumber.OP_EACH:
                return Expression.Call(
                    typeof(Builtins).GetMethod("HashEach"),
                    Runtime,
                    OpContext(op),
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_TEMPORARY:
            {
                Temporary tm = (Temporary)op;

                return GetTemporary(tm.Index, TypeForTemporary(tm.Slot));
            }
            case Opcode.OpNumber.OP_TEMPORARY_SET:
            {
                Temporary tm = (Temporary)op;
                Expression exp = Generate(sub, op.Childs[0]);

                return Expression.Assign(GetTemporary(tm.Index, TypeForTemporary(tm.Slot)), exp);
            }
            case Opcode.OpNumber.OP_TEMPORARY_CLEAR:
            {
                Temporary tm = (Temporary)op;
                var type = TypeForTemporary(tm.Slot);

                return Expression.Assign(GetTemporary(tm.Index, type),
                                         Expression.Constant(null, type));
            }
            case Opcode.OpNumber.OP_LEXICAL:
            {
                Lexical lx = (Lexical)op;

                if (lx.LexicalIndex == 0 && !IsMain)
                {
                    if ((op.Context & (int)Opcode.ContextValues.LVALUE) != 0)
                        ParameterLValue = true;

                    return Arguments;
                }
                else
                    return GetLexicalValue(lx.LexicalIndex, lx.Slot);
            }
            case Opcode.OpNumber.OP_LEXICAL_CLEAR:
            {
                Lexical lx = (Lexical)op;
                Expression lexvar = GetLexical(lx.LexicalIndex, lx.Slot);

                return Expression.Assign(lexvar, Expression.Constant(null, lexvar.Type));
            }
            case Opcode.OpNumber.OP_LEXICAL_SET:
            {
                Lexical lx = (Lexical)op;
                Expression lexvar = GetLexical(lx.LexicalIndex, lx.Slot);

                return Expression.Assign(
                    lexvar,
                    Utils.CheckedConvert(Generate(sub, op.Childs[0]), lexvar.Type));
            }
            case Opcode.OpNumber.OP_LEXICAL_PAD:
            {
                Lexical lx = (Lexical)op;

                return GetLexicalPadValue(lx.LexicalInfo);
            }
            case Opcode.OpNumber.OP_LEXICAL_PAD_CLEAR:
            {
                Lexical lx = (Lexical)op;
                Expression lexvar = GetLexicalPad(lx.LexicalInfo);

                return Expression.Assign(lexvar, Expression.Constant(null, lexvar.Type));
            }
            case Opcode.OpNumber.OP_LEXICAL_PAD_SET:
            {
                Lexical lx = (Lexical)op;
                Expression lexvar = GetLexicalPad(lx.LexicalInfo);

                return Expression.Assign(
                    lexvar,
                    Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_LOCALIZE_LEXICAL_PAD:
            {
                LocalLexical lx = (LocalLexical)op;
                Expression lexvar = GetLexicalPad(lx.LexicalInfo);
                var saved = GetTemporary(lx.Index, typeof(object));

                return Expression.Assign(saved, lexvar);
            }
            case Opcode.OpNumber.OP_RESTORE_LEXICAL_PAD:
            {
                var exps = new List<Expression>();
                LocalLexical lx = (LocalLexical)op;
                Expression lexvar = GetLexicalPad(lx.LexicalInfo);
                var saved = GetTemporary(lx.Index, typeof(object));

                exps.Add(
                    Expression.IfThen(
                        Expression.NotEqual(
                            saved,
                            Expression.Constant(null, saved.Type)),
                        Expression.Assign(lexvar, saved)));
                exps.Add(Expression.Assign(
                             saved,
                             Expression.Constant(null, saved.Type)));

                return Expression.Block(typeof(void), exps);
            }
            case Opcode.OpNumber.OP_LOCALIZE_LEXICAL:
            {
                LocalLexical lx = (LocalLexical)op;
                Expression lexvar = GetLexical(lx.LexicalIndex, TypeForSlot(lx.Slot));
                var saved = GetTemporary(lx.Index, typeof(object));

                return Expression.Assign(saved, lexvar);
            }
            case Opcode.OpNumber.OP_RESTORE_LEXICAL:
            {
                var exps = new List<Expression>();
                LocalLexical lx = (LocalLexical)op;
                Expression lexvar = GetLexical(lx.LexicalIndex, TypeForSlot(lx.Slot));
                var saved = GetTemporary(lx.Index, typeof(object));

                exps.Add(
                    Expression.IfThen(
                        Expression.NotEqual(
                            saved,
                            Expression.Constant(null, saved.Type)),
                        Expression.Assign(lexvar, saved)));
                exps.Add(Expression.Assign(
                             saved,
                             Expression.Constant(null, saved.Type)));

                return Expression.Block(typeof(void), exps);
            }
            case Opcode.OpNumber.OP_VEC:
            {
                return Expression.New(
                    typeof(P5Vec).GetConstructor(new[] { typeof(Runtime), typeof(IP5Any), typeof(IP5Any), typeof(IP5Any) }),
                    Runtime,
                    Generate(sub, op.Childs[0]),
                    Generate(sub, op.Childs[1]),
                    Generate(sub, op.Childs[2]));
            }
            case Opcode.OpNumber.OP_SUBSTR:
            {
                Expression value = Expression.Convert(
                    Generate(sub, op.Childs[0]), typeof(P5Scalar));
                Expression offset = Expression.Call(
                    Generate(sub, op.Childs[1]),
                    typeof(IP5Any).GetMethod("AsInteger"),
                    Runtime);
                Expression length = null;

                if (op.Childs.Length >= 3)
                    length = Expression.Call(
                        Generate(sub, op.Childs[2]),
                        typeof(IP5Any).GetMethod("AsInteger"),
                        Runtime);

                if (op.Childs.Length == 4)
                    return Expression.Call(
                        value,
                        typeof(P5Scalar).GetMethod("SpliceSubstring", new[] { typeof(Runtime), typeof(int), typeof(int), typeof(IP5Any) }),
                        Runtime,
                        offset, length,
                        Generate(sub, op.Childs[3]));
                else if (op.Childs.Length == 3)
                    return Expression.New(
                        typeof(P5Substr).GetConstructor(new[] { typeof(Runtime), typeof(IP5Any), typeof(int), typeof(int) }),
                        Runtime, value, offset, length);
                else if (op.Childs.Length == 2)
                    return Expression.New(
                        typeof(P5Substr).GetConstructor(new[] { typeof(Runtime), typeof(IP5Any), typeof(int) }),
                        Runtime, value, offset);

                throw new System.Exception(); // can't happen
            }
            case Opcode.OpNumber.OP_BLESS:
            {
                return Expression.Call(
                    typeof(Builtins).GetMethod("Bless"),
                    Runtime,
                    Utils.CheckedConvert(
                        Generate(sub, op.Childs[0]), typeof(P5Scalar)),
                    Expression.Call(
                        typeof(Builtins).GetMethod("ConvertToString"),
                        Runtime,
                        Generate(sub, op.Childs[1])));
            }
            case Opcode.OpNumber.OP_CALL_METHOD:
            {
                CallMethod cm = (CallMethod)op;

                return
                    Expression.Call(
                        typeof(Builtins).GetMethod("CallMethod"),
                        Runtime,
                        Generate(sub, op.Childs[0]),
                        OpContext(op),
                        Expression.Constant(cm.Method));
            }
            case Opcode.OpNumber.OP_CALL_METHOD_INDIRECT:
            {
                return
                    Expression.Call(
                        typeof(Builtins).GetMethod("CallMethodIndirect"),
                        Runtime,
                        Generate(sub, op.Childs[1]),
                        OpContext(op),
                        Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_FIND_METHOD:
            {
                CallMethod cm = (CallMethod)op;

                return
                    Expression.Call(
                        Generate(sub, op.Childs[0]),
                        typeof(IP5Any).GetMethod("FindMethod"),
                        Runtime, Expression.Constant(cm.Method));
            }
            case Opcode.OpNumber.OP_CALL:
            {
                var lexicals = new List<Lexical>();
                var code = Expression.Convert(
                    Generate(sub, op.Childs[1]), typeof(P5Code));

                foreach (var opc in op.Childs[0].Childs)
                {
                    if (opc.Number == Opcode.OpNumber.OP_LEXICAL)
                    {
                        Lexical lx = (Lexical)opc;

                        if (lx.Slot == Opcode.Sigil.SCALAR)
                            lexicals.Add(lx);
                    }
                }

                if (lexicals.Count != 0)
                {
                    var temp = Expression.Parameter(typeof(P5Code));
                    var exps = new List<Expression>();
                    var upgrade = new List<Expression>();

                    foreach (var lx in lexicals)
                        upgrade.Add(
                            Expression.Assign(
                                GetLexical(lx.LexicalIndex, lx.Slot),
                                Expression.Call(
                                    typeof(Builtins).GetMethod("UpgradeScalar"),
                                    Runtime,
                                    GetLexical(lx.LexicalIndex, lx.Slot))));

                    exps.Add(Expression.Assign(temp, code));
                    exps.Add(
                        Expression.IfThen(
                            Expression.Property(
                                temp,
                                typeof(P5Code).GetProperty("LValueParameters")),
                            Expression.Block(upgrade)));
                    exps.Add(
                        Expression.Call(
                            code, typeof(P5Code).GetMethod("Call"),
                            Runtime, OpContext(op),
                            Generate(sub, op.Childs[0])));

                    return Expression.Block(
                        typeof(object),
                        new ParameterExpression[] { temp },
                        exps);
                }
                else
                    return Expression.Call(
                        code, typeof(P5Code).GetMethod("Call"),
                        Runtime, OpContext(op),
                        Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_REFTYPE:
                return Builtin(sub, op, "ReferenceType", 0);
            case Opcode.OpNumber.OP_REFERENCE:
            {
                return Expression.New(
                    typeof(P5Scalar).GetConstructor(
                        new Type[] { typeof(Runtime), typeof(IP5Referrable) }),
                    new Expression[] {
                        Runtime,
                        Expression.Call(
                            typeof(Builtins).GetMethod("UpgradeReferrable"),
                            Runtime,
                            Generate(sub, op.Childs[0])),
                    });
            }
            case Opcode.OpNumber.OP_VIVIFY_SCALAR:
                // TODO test/handle aliasing
                return Expression.Call(
                    Expression.Convert(
                        Generate(sub, op.Childs[0]),
                        typeof(IP5Any)),
                    typeof(IP5Any).GetMethod("VivifyScalar"),
                    Runtime);
            case Opcode.OpNumber.OP_VIVIFY_ARRAY:
                // TODO test/handle aliasing
                return Expression.Call(
                    Expression.Convert(
                        Generate(sub, op.Childs[0]),
                        typeof(IP5Any)),
                    typeof(IP5Any).GetMethod("VivifyArray"),
                    Runtime);
            case Opcode.OpNumber.OP_VIVIFY_HASH:
                // TODO test/handle aliasing
                return Expression.Call(
                    Expression.Convert(
                        Generate(sub, op.Childs[0]),
                        typeof(IP5Any)),
                    typeof(IP5Any).GetMethod("VivifyHash"),
                    Runtime);
            case Opcode.OpNumber.OP_DEREFERENCE_SCALAR:
                return Builtin(sub, op, "DereferenceScalar", 0);
            case Opcode.OpNumber.OP_DEREFERENCE_ARRAY:
                return Builtin(sub, op, "DereferenceArray", 0);
            case Opcode.OpNumber.OP_DEREFERENCE_HASH:
                return Builtin(sub, op, "DereferenceHash", 0);
            case Opcode.OpNumber.OP_DEREFERENCE_GLOB:
                return Builtin(sub, op, "DereferenceGlob", 0);
            case Opcode.OpNumber.OP_DEREFERENCE_SUB:
                return Builtin(sub, op, "DereferenceSubroutine", 0);
            case Opcode.OpNumber.OP_MAKE_CLOSURE:
            {
                return Expression.Call(
                    Generate(sub, op.Childs[0]),
                    typeof(P5Code).GetMethod("MakeClosure"),
                    Runtime, Pad);
            }
            case Opcode.OpNumber.OP_MAKE_QR:
            {
                return Expression.New(
                    typeof(P5Scalar).GetConstructor(new System.Type[] { typeof(Runtime), typeof(IP5Referrable) }),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_LOCALIZE_GLOB_SLOT:
            {
                var exps = new List<Expression>();
                var vars = new List<ParameterExpression>();
                var lop = (LocalGlobSlot)op;
                var st = typeof(Runtime).GetField("SymbolTable");
                var glob = Expression.Variable(typeof(P5Typeglob));
                var saved = Expression.Variable(TypeForSlot(lop.Slot));
                var temp = GetTemporary(lop.Index, typeof(IP5Any));

                // FIXME do not walk twice the symbol table
                exps.Add(
                    Expression.Assign(
                        glob,
                        Expression.Call(
                            Expression.Field(Runtime, st),
                            typeof(P5SymbolTable).GetMethod("GetGlob"),
                            Runtime,
                            Expression.Constant(lop.Name),
                            Expression.Constant(true))));
                exps.Add(
                    Expression.Assign(
                        temp,
                        Expression.Call(
                            Expression.Field(Runtime, st),
                            typeof(P5SymbolTable).GetMethod(MethodForSlot(lop.Slot)),
                            Runtime,
                            Expression.Constant(lop.Name),
                            Expression.Constant(true))));
                exps.Add(
                    Expression.Assign(
                        saved,
                        Expression.Convert(
                            Expression.Call(
                                temp,
                                typeof(IP5Any).GetMethod("Localize"),
                                Runtime),
                            saved.Type)));
                exps.Add(
                    Expression.Assign(
                        Expression.Property(
                            glob,
                            PropertyForSlot(lop.Slot)),
                        saved));
                exps.Add(saved);

                vars.Add(glob);
                vars.Add(saved);

                return Expression.Block(typeof(object), vars, exps);
            }
            case Opcode.OpNumber.OP_RESTORE_GLOB_SLOT:
            {
                var exps = new List<Expression>();
                var vars = new List<ParameterExpression>();
                var lop = (LocalGlobSlot)op;
                var st = typeof(Runtime).GetField("SymbolTable");
                var glob = Expression.Variable(typeof(P5Typeglob));
                var saved = GetTemporary(lop.Index, typeof(IP5Any));

                exps.Add(
                    Expression.Assign(
                        glob,
                        Expression.Call(
                            Expression.Field(Runtime, st),
                            typeof(P5SymbolTable).GetMethod("GetGlob"),
                            Runtime,
                            Expression.Constant(lop.Name),
                            Expression.Constant(true))));
                exps.Add(
                    Expression.Assign(
                        Expression.Property(
                            glob,
                            PropertyForSlot(lop.Slot)),
                        Expression.Convert(
                            saved,
                            TypeForSlot(lop.Slot))));
                exps.Add(
                    Expression.Assign(
                        saved,
                        Expression.Constant(null, saved.Type)));

                vars.Add(glob);

                return Expression.IfThen(
                    Expression.NotEqual(
                        saved,
                        Expression.Constant(null, typeof(object))),
                    Expression.Block(typeof(object), vars, exps));
            }
            case Opcode.OpNumber.OP_LOCALIZE_ARRAY_ELEMENT:
            {
                var le = (LocalElement)op;

                return Expression.Call(
                    typeof(Builtins).GetMethod("LocalizeArrayElement"),
                    Runtime,
                    Generate(sub, le.Childs[0]),
                    Generate(sub, le.Childs[1]),
                    GetTemporary(le.Index, typeof(SavedValue)));
            }
            case Opcode.OpNumber.OP_RESTORE_ARRAY_ELEMENT:
            {
                var le = (LocalElement)op;

                return Expression.Call(
                    typeof(Builtins).GetMethod("RestoreArrayElement"),
                    Runtime,
                    GetTemporary(le.Index, typeof(SavedValue)));
            }
            case Opcode.OpNumber.OP_LOCALIZE_HASH_ELEMENT:
            {
                var le = (LocalElement)op;

                return Expression.Call(
                    typeof(Builtins).GetMethod("LocalizeHashElement"),
                    Runtime,
                    Generate(sub, le.Childs[0]),
                    Generate(sub, le.Childs[1]),
                    GetTemporary(le.Index, typeof(SavedValue)));
            }
            case Opcode.OpNumber.OP_RESTORE_HASH_ELEMENT:
            {
                var le = (LocalElement)op;

                return Expression.Call(
                    typeof(Builtins).GetMethod("RestoreHashElement"),
                    Runtime,
                    GetTemporary(le.Index, typeof(SavedValue)));
            }
            case Opcode.OpNumber.OP_LEXICAL_STATE_SET:
            {
                var ls = (LexState)op;
                var state = sub.LexicalStates[ls.Index];

                // force package creation
                if (state.Package != null)
                    DefinePackage(state.Package);

                return Expression.Block(
                    typeof(void),
                    Expression.Assign(
                        Expression.Field(Runtime, "Package"),
                        Expression.Constant(state.Package)),
                    Expression.Assign(
                        Expression.Field(Runtime, "Hints"),
                        Expression.Constant(state.Hints)));
            }
            case Opcode.OpNumber.OP_LEXICAL_STATE_SAVE:
            {
                var ls = (LexState)op;
                var slot = GetSavedLexState(ls.Index);

                return Expression.Block(
                    typeof(void),
                    Expression.Assign(
                        Expression.Field(slot, "Package"),
                        Expression.Field(Runtime, "Package")),
                    Expression.Assign(
                        Expression.Field(slot, "Hints"),
                        Expression.Field(Runtime, "Hints")));
            }
            case Opcode.OpNumber.OP_LEXICAL_STATE_RESTORE:
            {
                var ls = (LexState)op;
                var slot = GetSavedLexState(ls.Index);

                return Expression.Block(
                    typeof(void),
                    Expression.Assign(
                        Expression.Field(Runtime, "Package"),
                        Expression.Field(slot, "Package")),
                    Expression.Assign(
                        Expression.Field(Runtime, "Hints"),
                        Expression.Field(slot, "Hints")));
            }
            case Opcode.OpNumber.OP_CALLER:
            {
                return op.Childs.Length == 0 ?
                    Expression.Call(
                        Runtime,
                        typeof(Runtime).GetMethod("CallerNoArg"),
                        OpContext(op)) :
                    Expression.Call(
                        Runtime,
                        typeof(Runtime).GetMethod("CallerWithArg"),
                        Generate(sub, op.Childs[0]),
                        OpContext(op));
            }
            case Opcode.OpNumber.OP_CONSTANT_REGEX:
            {
                var cs = (ConstantSub)op;

                return ConstantRegex(cs.Value);
            }
            case Opcode.OpNumber.OP_EVAL_REGEX:
            {
                RegexEval re = (RegexEval)op;

                return Expression.Call(
                    typeof(Builtins).GetMethod("CompileRegex"),
                    Runtime,
                    Generate(sub, re.Childs[0]),
                    Expression.Constant(re.Flags));
            }
            case Opcode.OpNumber.OP_POS:
            {
                return Expression.New(
                    typeof(P5Pos).GetConstructor(ProtoRuntimeAny),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            }
            case Opcode.OpNumber.OP_RX_STATE_RESTORE:
            {
                RegexState rs = (RegexState)op;

                return Expression.Assign(
                    Expression.Field(Runtime, "LastMatch"),
                    GetSavedRxState(rs.Index));
            }
            case Opcode.OpNumber.OP_MATCH:
            {
                RegexMatch rm = (RegexMatch)op;
                bool global = (rm.Flags & Opcode.RX_GLOBAL) != 0;
                var meth = typeof(IP5Regex).GetMethod(global ? "MatchGlobal" : "Match");

                return Expression.Call(
                    Generate(sub, op.Childs[1]),
                    meth,
                    Runtime,
                    Generate(sub, op.Childs[0]),
                    Expression.Constant(rm.Flags & Opcode.RX_KEEP),
                    OpContext(rm),
                    GetSavedRxState(rm.Index));
            }
            case Opcode.OpNumber.OP_REPLACE:
            {
                RegexReplace rm = (RegexReplace)op;
                bool global = (rm.Flags & Opcode.RX_GLOBAL) != 0;

                if (global)
                    return GenerateGlobalSubstitution(sub, rm);
                else
                    return GenerateSubstitution(sub, rm);
            }
            case Opcode.OpNumber.OP_RX_SPLIT_SKIPSPACES:
                return Expression.Call(
                    typeof(Builtins).GetMethod("SplitSpaces"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_TRANSLITERATE:
            {
                RegexTransliterate rt = (RegexTransliterate)op;

                return
                    Expression.New(
                        typeof(P5Scalar).GetConstructor(ProtoRuntimeInt),
                        Runtime,
                        Expression.Call(
                            typeof(Builtins).GetMethod("Transliterate"),
                            Runtime,
                            Generate(sub, rt.Childs[0]),
                            Expression.Constant(rt.Match),
                            Expression.Constant(rt.Replacement),
                            Expression.Constant(rt.Flags)));
            }
            case Opcode.OpNumber.OP_UNLINK:
                return Expression.Call(
                    typeof(Builtins).GetMethod("Unlink"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_OPEN:
                return Expression.Call(
                    typeof(Builtins).GetMethod("Open"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_CLOSE:
                return Expression.Call(
                    typeof(Builtins).GetMethod("Close"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            case Opcode.OpNumber.OP_FT_ISFILE:
                return Expression.Call(
                    typeof(Builtins).GetMethod("IsFile"),
                    Runtime,
                    Generate(sub, op.Childs[0]));
            default:
                throw new System.Exception(string.Format("Unhandled opcode {0:S} in generation", op.Number.ToString()));
            }
        }

        private Expression GenerateGlobalSubstitution(Subroutine sub, RegexReplace rm)
        {
            var scalar = Expression.Variable(typeof(P5Scalar));
            var init_scalar =
                Expression.Assign(scalar, Generate(sub, rm.Childs[0]));
            var pos = Expression.Variable(typeof(int));
            var count = Expression.Variable(typeof(int));
            var matched = Expression.Variable(typeof(bool));
            var str = Expression.Variable(typeof(string));
            var replace = Expression.Variable(typeof(string));
            var repl_list = Expression.Variable(typeof(List<RxReplacement>));
            var init_str =
                Expression.Assign(
                    str,
                    Expression.Call(
                        scalar,
                        typeof(IP5Any).GetMethod("AsString"),
                        Runtime));
            var match = Expression.Call(
                Generate(sub, rm.Childs[1]),
                typeof(IP5Regex).GetMethod("MatchString"),
                Runtime,
                str,
                pos,
                Expression.Constant(false),
                GetSavedRxState(rm.Index));
            var rxstate = Expression.Field(
                Runtime,
                typeof(Runtime).GetField("LastMatch"));
            var rx_end =
                Expression.Field(
                    rxstate,
                    typeof(RxResult).GetField("End"));
            var rx_start =
                Expression.Field(
                    rxstate,
                    typeof(RxResult).GetField("Start"));

            var if_match = new List<Expression>();

            if_match.Add(Expression.PreIncrementAssign(count));
            if_match.Add(Expression.Assign(matched, Expression.Constant(true)));
            if_match.Add(Expression.Assign(pos, rx_end));

            // at this point all nested scopes have been generated
            if_match.Add(
                Expression.Assign(
                    replace,
                    Expression.Call(
                        typeof(Builtins).GetMethod("ConvertToString"),
                        Runtime,
                        ValueBlocks[rm.To])));

            if_match.Add(
                Expression.Call(
                    repl_list,
                    typeof(List<RxReplacement>).GetMethod("Add"),
                    Expression.New(
                        typeof(RxReplacement).GetConstructor(
                            new Type[] { typeof(string), typeof(int), typeof(int) }),
                        replace, rx_start, rx_end)));

            var break_to = Expression.Label(typeof(void));
            var loop =
                Expression.Loop(
                    Expression.Block(
                        Expression.IfThenElse(
                            match,
                            Expression.Block(typeof(void), if_match),
                            Expression.Break(break_to))),
                    break_to);

            // TODO save last match

            var vars = new List<ParameterExpression>();
            vars.Add(scalar);
            vars.Add(pos);
            vars.Add(count);
            vars.Add(matched);
            vars.Add(str);
            vars.Add(replace);
            vars.Add(repl_list);

            var body = new List<Expression>();
            body.Add(init_scalar);
            body.Add(init_str);
            body.Add(Expression.Assign(pos, Expression.Constant(-1)));
            body.Add(Expression.Assign(count, Expression.Constant(0)));
            body.Add(Expression.Assign(matched, Expression.Constant(false)));
            body.Add(Expression.Assign(
                         repl_list,
                         Expression.New(
                             typeof(List<RxReplacement>).GetConstructor(
                                 new Type[0]))));
            body.Add(loop);

            // replace substrings
            body.Add(
                Expression.Call(
                    typeof(P5Regex).GetMethod("ReplaceSubstrings"),
                    Runtime,
                    scalar,
                    str,
                    repl_list));

            // return value
            var result =
                Expression.New(
                    typeof(P5Scalar).GetConstructor(ProtoRuntimeInt),
                    Runtime,
                    count);

            body.Add(
                Expression.Condition(
                    Expression.Equal(
                        OpContext(rm),
                        Expression.Constant(Opcode.ContextValues.LIST)),
                    Expression.New(
                        typeof(P5List).GetConstructor(ProtoRuntimeAny),
                        Runtime,
                        result),
                    result, typeof(IP5Any)));

            return Expression.Block(typeof(object), vars, body);
        }

        private Expression GenerateSubstitution(Subroutine sub, RegexReplace rm)
        {
            var scalar = Expression.Variable(typeof(P5Scalar));
            var str = Expression.Variable(typeof(string));
            var replace = Expression.Variable(typeof(IP5Any));
            var init_scalar =
                Expression.Assign(scalar, Generate(sub, rm.Childs[0]));
            var init_str =
                Expression.Assign(
                    str,
                    Expression.Call(
                        scalar,
                        typeof(IP5Any).GetMethod("AsString"),
                        Runtime));

            var replace_list = new List<Expression>();

            // at this point all nested scopes have been generated
            replace_list.Add(
                Expression.Assign(
                    replace,
                    // TODO use ConvertToString after fixing SpliceSubstring
                    Expression.Call(
                        typeof(Builtins).GetMethod("UpgradeScalar"),
                        Runtime,
                        ValueBlocks[rm.To])));

            // replace in string
            var rxstate = Expression.Field(
                Runtime,
                typeof(Runtime).GetField("LastMatch"));

            replace_list.Add(
                Expression.Call(
                    scalar,
                    typeof(P5Scalar).GetMethod("SpliceSubstring", new[] { typeof(Runtime), typeof(int), typeof(int), typeof(IP5Any) }),
                    Runtime,
                    Expression.Field(
                        rxstate,
                        typeof(RxResult).GetField("Start")),
                    Expression.Subtract(
                        Expression.Field(
                            rxstate,
                            typeof(RxResult).GetField("End")),
                        Expression.Field(
                            rxstate,
                            typeof(RxResult).GetField("Start"))),
                    replace));

            // return true at end of replacement
            replace_list.Add(Expression.Constant(true));

            var match = Expression.Call(
                Generate(sub, rm.Childs[1]),
                typeof(IP5Regex).GetMethod("MatchString"),
                Runtime,
                str,
                Expression.Constant(-1),
                Expression.Constant(false),
                GetSavedRxState(rm.Index));
            var repl = Expression.Condition(
                match,
                Expression.Block(typeof(bool), replace_list),
                Expression.Constant(false));

            var vars = new List<ParameterExpression>();
            vars.Add(scalar);
            vars.Add(str);
            vars.Add(replace);

            var exps = new List<Expression>();
            exps.Add(init_scalar);
            exps.Add(init_str);
            exps.Add(
                Expression.New(
                    typeof(P5Scalar).GetConstructor(ProtoRuntimeBool),
                    Runtime,
                    repl));

            return Expression.Block(typeof(P5Scalar), vars, exps);
        }

        public bool AssignParameters { get { return ParameterLValue; } }

        private LabelTarget SubLabel;
        private ParameterExpression Runtime, Arguments, Context, Pad;
        private List<ParameterExpression> Variables, Lexicals, Temporaries, LexStates, RxStates;
        private Dictionary<BasicBlock, LabelTarget> BlockLabels;
        private Dictionary<BasicBlock, Expression> ValueBlocks;
        private bool IsMain, ParameterLValue;
    }
}
