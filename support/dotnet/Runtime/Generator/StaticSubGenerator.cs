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
    internal class StaticSubGenerator : SubGenerator
    {
        private static Type[] ProtoRuntime =
            new Type[] { typeof(Runtime) };
        private static Type[] ProtoRuntimeInt =
            new Type[] { typeof(Runtime), typeof(int) };
        private static Type[] ProtoRuntimeDouble =
            new Type[] { typeof(Runtime), typeof(double) };
        private static Type[] ProtoRuntimeExpressionType =
            new Type[] { typeof(Runtime), typeof(ExpressionType) };
        private static Type[] ProtoRuntimeContextValuesBool =
            new Type[] { typeof(Runtime), typeof(Opcode.ContextValues), typeof(bool) };

        internal StaticSubGenerator(StaticModuleGenerator module_generator,
                                    Dictionary<Subroutine, StaticModuleGenerator.SubInfo> subroutines)
        {
            ModuleGenerator = module_generator;
            Subroutines = subroutines;
        }

        protected override Expression Builtin(Subroutine sub, Opcode op, string prefix, int count, params Expression[] extra)
        {
            return null;
        }

        protected Expression UnaryOperator(Subroutine sub, Opcode op, Expression binder)
        {
            var delegateType = typeof(Func<CallSite, object, object>);
            var siteType = typeof(CallSite<Func<CallSite, object, object>>);
            var initExpr = Expression.Call(
                siteType.GetMethod("Create"),
                binder);
            var staticField = ModuleGenerator.AddField(initExpr, siteType);

            var res =
                Expression.Call(
                    Expression.Field(
                        Expression.Field(null, staticField),
                        siteType.GetField("Target")
                        ),
                    delegateType.GetMethod("Invoke"),
                    Expression.Field(null, staticField),
                    Generate(sub, op.Childs[0]));

            return Expression.Convert(res, typeof(IP5Any));
        }

        protected override Expression UnaryOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return UnaryOperator(
                sub, op,
                Expression.New(
                    typeof(P5UnaryOperationBinder).GetConstructor(ProtoRuntimeExpressionType),
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(operation)));
        }

        protected override Expression UnaryIncrement(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return UnaryOperator(
                sub, op,
                Expression.New(
                    typeof(P5UnaryIncrementBinder).GetConstructor(ProtoRuntimeExpressionType),
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(operation)));
        }

        protected Expression BinaryOperator<Result>(Subroutine sub, Expression left, Expression right, Expression binder)
        {
            var delegateType = typeof(Func<CallSite, object, object, Result>);
            var siteType = typeof(CallSite<Func<CallSite, object, object, Result>>);
            var initExpr = Expression.Call(
                siteType.GetMethod("Create"),
                binder);
            var staticField = ModuleGenerator.AddField(initExpr, siteType);

            var res =
                Expression.Call(
                    Expression.Field(
                        Expression.Field(null, staticField),
                        siteType.GetField("Target")
                        ),
                    delegateType.GetMethod("Invoke"),
                    Expression.Field(null, staticField),
                    left,
                    right);

            if (res.Type == typeof(object))
                return Expression.Convert(res, typeof(IP5Any));
            else
                return res;
        }

        protected Expression BinaryOperator<Result>(Subroutine sub, Opcode op, Expression binder)
        {
            var left = Generate(sub, op.Childs[0]);
            var right = Generate(sub, op.Childs[1]);

            return BinaryOperator<Result>(sub, left, right, binder);
        }

        protected override Expression StringOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op,
                Expression.New(
                    typeof(P5StringOperationBinder).GetConstructor(new[] { typeof(ExpressionType), typeof(Runtime) }),
                    Expression.Constant(operation),
                    ModuleGenerator.InitRuntime));
        }

        protected override Expression BinaryOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op,
                Expression.New(
                    typeof(P5BinaryOperationBinder).GetConstructor(ProtoRuntimeExpressionType),
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(operation)));
        }

        protected override Expression NumericRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op,
                Expression.New(
                    typeof(P5NumericCompareBinder).GetConstructor(ProtoRuntimeExpressionType),
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(operation)));
        }

        protected override Expression ConvertBoolean(Subroutine sub, Opcode op)
        {
            return null;
        }

        protected override Expression Iterator(Subroutine sub, Expression value)
        {
            return null;
        }

        protected override Expression StringRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op,
                Expression.New(
                    typeof(P5StringCompareBinder).GetConstructor(ProtoRuntimeExpressionType),
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(operation)));
        }

        protected override Expression ScalarAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue)
        {
            return BinaryOperator<P5Scalar>(
                sub, lvalue, rvalue,
                Expression.New(
                    typeof(P5ScalarAssignmentBinder).GetConstructor(ProtoRuntime),
                    ModuleGenerator.InitRuntime));
        }

        protected override Expression ArrayAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue, bool common)
        {
            return BinaryOperator<object>(
                sub, lvalue, rvalue,
                Expression.New(
                    typeof(P5ArrayAssignmentBinder).GetConstructor(ProtoRuntimeContextValuesBool),
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(cxt),
                    Expression.Constant(common)));
        }

        protected override Expression ArrayItem(Subroutine sub, Opcode.ContextValues cxt, Expression value, Expression index, bool create)
        {
            return null;
        }

        protected override Expression ArrayItemAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression index, Expression rvalue)
        {
            return null;
        }

        protected override Expression HashItem(Subroutine sub, Opcode.ContextValues cxt, Expression value, Expression index, bool create)
        {
            return null;
        }

        protected override Expression HashItemAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression index, Expression rvalue)
        {
            return null;
        }

        protected override Expression Defined(Subroutine sub, Opcode op)
        {
            return UnaryOperator(
                sub, op,
                Expression.New(
                    typeof(P5DefinedBinder).GetConstructor(ProtoRuntime),
                    ModuleGenerator.InitRuntime));
        }

        protected override void DefinePackage(string pack)
        {
            ModuleGenerator.AddInitPackage(pack);
        }

        protected override Expression ConstantInteger(int value)
        {
            var ctor = typeof(P5Scalar).GetConstructor(ProtoRuntimeInt);
            var init = Expression.New(
                ctor,
                new Expression[] {
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(value) });
            FieldInfo field = ModuleGenerator.AddField(init);

            return Expression.Field(null, field);
        }

        protected override Expression ConstantFloat(double value)
        {
            var ctor = typeof(P5Scalar).GetConstructor(ProtoRuntimeDouble);
            var init = Expression.New(
                ctor,
                new Expression[] {
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(value) });
            FieldInfo field = ModuleGenerator.AddField(init);

            return Expression.Field(null, field);
        }

        protected override Expression ConstantSub(Subroutine sub)
        {
            return Expression.Field(null, Subroutines[sub].CodeField);
        }

        protected override Expression ConstantRegex(Subroutine sub)
        {
            return Expression.Field(null, Subroutines[sub].CodeField);
        }

        protected override Expression AccessGlobal(Expression runtime_exp, Opcode.Sigil slot, string name, bool create)
        {
            var st = typeof(Runtime).GetField("SymbolTable");
            Expression global;

            if (!create || slot == Opcode.Sigil.STASH)
                global = Expression.Call(
                    Expression.Field(runtime_exp, st),
                    typeof(P5SymbolTable).GetMethod(MethodForSlot(slot)),
                    runtime_exp,
                    Expression.Constant(name),
                    Expression.Constant(create));
            else
            {
                var field = ModuleGenerator.AccessGlob(
                    name,
                    Expression.Call(
                        Expression.Field(ModuleGenerator.InitRuntime, st),
                        typeof(P5SymbolTable).GetMethod("GetGlob"),
                        ModuleGenerator.InitRuntime,
                        Expression.Constant(name),
                        Expression.Constant(create)));

                global = Expression.Field(null, field);

                if (slot != Opcode.Sigil.GLOB)
                    global = Expression.Call(
                        global,
                        typeof(P5Typeglob).GetMethod(MethodForSlot(slot)),
                        runtime_exp);
            }

            return global;
        }

        private StaticModuleGenerator ModuleGenerator;
        private Dictionary<Subroutine, StaticModuleGenerator.SubInfo> Subroutines;
    }
}
