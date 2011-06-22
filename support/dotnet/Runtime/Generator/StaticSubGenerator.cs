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
        private static Type[] ProtoRuntimeInt =
            new Type[] { typeof(Runtime), typeof(int) };
        private static Type[] ProtoRuntimeDouble =
            new Type[] { typeof(Runtime), typeof(double) };

        internal StaticSubGenerator(StaticModuleGenerator module_generator,
                                    Dictionary<Subroutine, StaticModuleGenerator.SubInfo> subroutines)
        {
            ModuleGenerator = module_generator;
            Subroutines = subroutines;
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
                    typeof(P5UnaryOperationBinder).GetConstructor(new[] { typeof(ExpressionType), typeof(Runtime) }),
                    Expression.Constant(operation),
                    ModuleGenerator.InitRuntime));
        }

        protected override Expression UnaryIncrement(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return UnaryOperator(
                sub, op,
                Expression.New(
                    typeof(P5UnaryIncrementBinder).GetConstructor(new[] { typeof(ExpressionType), typeof(Runtime) }),
                    Expression.Constant(operation),
                    ModuleGenerator.InitRuntime));
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

        protected override Expression BinaryOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op,
                Expression.New(
                    typeof(P5BinaryOperationBinder).GetConstructor(new[] { typeof(ExpressionType), typeof(Runtime) }),
                    Expression.Constant(operation),
                    ModuleGenerator.InitRuntime));
        }

        protected override Expression NumericRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op,
                Expression.New(
                    typeof(P5NumericCompareBinder).GetConstructor(new[] { typeof(ExpressionType), typeof(Runtime) }),
                    Expression.Constant(operation),
                    ModuleGenerator.InitRuntime));
        }

        protected override Expression StringRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op,
                Expression.New(
                    typeof(P5StringCompareBinder).GetConstructor(new[] { typeof(ExpressionType), typeof(Runtime) }),
                    Expression.Constant(operation),
                    ModuleGenerator.InitRuntime));
        }

        protected override Expression ScalarAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue)
        {
            return BinaryOperator<P5Scalar>(
                sub, lvalue, rvalue,
                Expression.New(
                    typeof(P5ScalarAssignmentBinder).GetConstructor(new Type[] { typeof(Runtime) }),
                    ModuleGenerator.InitRuntime));
        }

        protected override Expression ArrayAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue, bool common)
        {
            return BinaryOperator<object>(
                sub, lvalue, rvalue,
                Expression.New(
                    typeof(P5ArrayAssignmentBinder).GetConstructor(new Type[] { typeof(Runtime), typeof(Opcode.ContextValues), typeof(bool) }),
                    ModuleGenerator.InitRuntime,
                    Expression.Constant(cxt),
                    Expression.Constant(common)));
        }

        protected override Expression Defined(Subroutine sub, Opcode op)
        {
            return UnaryOperator(
                sub, op,
                Expression.New(
                    typeof(P5DefinedBinder).GetConstructor(new[] { typeof(Runtime) }),
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
            var global =
                Expression.Call(
                    Expression.Field(runtime_exp, st),
                    typeof(P5SymbolTable).GetMethod(MethodForSlot(slot)),
                    runtime_exp,
                    Expression.Constant(name),
                    Expression.Constant(create));

            return global;
        }

        private StaticModuleGenerator ModuleGenerator;
        private Dictionary<Subroutine, StaticModuleGenerator.SubInfo> Subroutines;
    }
}
