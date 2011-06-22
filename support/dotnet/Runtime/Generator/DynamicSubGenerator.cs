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
    internal class DynamicSubGenerator : SubGenerator
    {
        internal DynamicSubGenerator(Runtime _runtime, DynamicModuleGenerator _module_generator)
        {
            runtime = _runtime;
            module_generator = _module_generator;
        }

        protected override Expression ConstantInteger(int value)
        {
            return Expression.Constant(new P5Scalar(runtime, value));
        }

        protected override Expression ConstantFloat(double value)
        {
            return Expression.Constant(new P5Scalar(runtime, value));
        }

        protected override Expression ConstantSub(Subroutine sub)
        {
            return Expression.Constant(module_generator.GetSubroutine(sub));
        }

        protected override Expression ConstantRegex(Subroutine sub)
        {
            return Expression.Constant(module_generator.GetRegex(sub));
        }

        // TODO duplicated in StaticSubGenerator
        protected Expression UnaryOperator(Subroutine sub, Opcode op, CallSiteBinder binder)
        {
            var delegateType = typeof(Func<CallSite, object, object>);
            var siteType = typeof(CallSite<Func<CallSite, object, object>>);
            var site = CallSite<Func<CallSite, object, object>>.Create(binder);

            var res =
                Expression.Call(
                    Expression.Field(
                        Expression.Constant(site),
                        siteType.GetField("Target")),
                    delegateType.GetMethod("Invoke"),
                    Expression.Constant(site),
                    Generate(sub, op.Childs[0]));

            return Expression.Convert(res, typeof(IP5Any));
        }

        protected override Expression UnaryOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return UnaryOperator(
                sub, op, new P5UnaryOperationBinder(operation, runtime));
        }

        protected override Expression UnaryIncrement(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return UnaryOperator(
                sub, op, new P5UnaryIncrementBinder(operation, runtime));
        }

        // TODO duplicated in StaticSubGenerator
        protected Expression BinaryOperator<Result>(Subroutine sub, Expression left, Expression right, CallSiteBinder binder)
        {
            var delegateType = typeof(Func<CallSite, object, object, Result>);
            var siteType = typeof(CallSite<Func<CallSite, object, object, Result>>);
            var site = CallSite<Func<CallSite, object, object, Result>>.Create(binder);

            var res =
                Expression.Call(
                    Expression.Field(
                        Expression.Constant(site),
                        siteType.GetField("Target")
                        ),
                    delegateType.GetMethod("Invoke"),
                    Expression.Constant(site),
                    left,
                    right);

            if (res.Type == typeof(object))
                return Expression.Convert(res, typeof(IP5Any));
            else
                return res;
        }

        protected Expression BinaryOperator<Result>(Subroutine sub, Opcode op, CallSiteBinder binder)
        {
            var left = Generate(sub, op.Childs[0]);
            var right = Generate(sub, op.Childs[1]);

            return BinaryOperator<Result>(sub, left, right, binder);
        }

        protected override Expression BinaryOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op, new P5BinaryOperationBinder(operation, runtime));
        }

        protected override Expression NumericRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op, new P5NumericCompareBinder(operation, runtime));
        }

        protected override Expression StringRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op, new P5StringCompareBinder(operation, runtime));
        }

        protected override Expression ScalarAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue)
        {
            return BinaryOperator<P5Scalar>(
                sub, lvalue, rvalue,
                new P5ScalarAssignmentBinder(runtime));
        }

        protected override Expression ArrayAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue, bool common)
        {
            return BinaryOperator<object>(
                sub, lvalue, rvalue,
                new P5ArrayAssignmentBinder(runtime, cxt, common));
        }

        protected override Expression Defined(Subroutine sub, Opcode op)
        {
            return UnaryOperator(
                sub, op, new P5DefinedBinder(runtime));
        }

        protected override void DefinePackage(string pack)
        {
            runtime.SymbolTable.GetPackage(runtime, pack, true);
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
                var glob = runtime.SymbolTable.GetGlob(runtime, name, true);

                global = Expression.Constant(glob);

                if (slot != Opcode.Sigil.GLOB)
                    global = Expression.Call(
                        global,
                        typeof(P5Typeglob).GetMethod(MethodForSlot(slot)),
                        runtime_exp);
            }

            return global;
        }

        Runtime runtime;
        DynamicModuleGenerator module_generator;
    }
}
