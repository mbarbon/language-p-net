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

        protected override Expression Builtin(Subroutine sub, Opcode op, string prefix, int count, params Expression[] extra)
        {
            var binder = new P5BuiltinBinder(runtime, prefix, count);
            var exps = new Expression[op.Childs.Length + 1 + extra.Length];
            int index = 1;

            foreach (var child in op.Childs)
                exps[index++] = Generate(sub, child);
            foreach (var exp in extra)
                exps[index++] = Expression.Convert(exp, typeof(object));

            var res = Utils.GenerateCall(exps, binder);

            if (res.Type == typeof(int) ||
                res.Type == typeof(bool) ||
                res.Type == typeof(double) ||
                res.Type == typeof(string))
                return Expression.Convert(res, typeof(object));
            else
                return res;
        }

        // TODO duplicated in StaticSubGenerator
        protected Expression UnaryOperator<Result>(Expression value, CallSiteBinder binder)
        {
            var delegateType = typeof(Func<CallSite, object, Result>);
            var siteType = typeof(CallSite<Func<CallSite, object, Result>>);
            var site = CallSite<Func<CallSite, object, Result>>.Create(binder);

            var res =
                Expression.Call(
                    Expression.Field(
                        Expression.Constant(site),
                        siteType.GetField("Target")),
                    delegateType.GetMethod("Invoke"),
                    Expression.Constant(site),
                    value);

            if (res.Type == typeof(object) && typeof(Result) != typeof(object))
                return Expression.Convert(res, typeof(Result));
            else
                return res;
        }

        protected Expression UnaryOperator<Result>(Subroutine sub, Opcode op, CallSiteBinder binder)
        {
            return UnaryOperator<Result>(Generate(sub, op.Childs[0]), binder);
        }

        protected override Expression UnaryOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return UnaryOperator<object>(
                sub, op, new P5UnaryOperationBinder(runtime, operation));
        }

        protected override Expression UnaryIncrement(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return UnaryOperator<object>(
                sub, op, new P5UnaryIncrementBinder(runtime, operation));
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

            if (res.Type == typeof(object) && typeof(Result) != typeof(object))
                return Expression.Convert(res, typeof(Result));
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
                sub, op, new P5BinaryOperationBinder(runtime, operation));
        }

        protected override Expression StringOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op, new P5StringOperationBinder(operation, runtime));
        }

        protected override Expression NumericRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op, new P5NumericCompareBinder(runtime, operation));
        }

        protected override Expression StringRelOperator(Subroutine sub, Opcode op, ExpressionType operation)
        {
            return BinaryOperator<object>(
                sub, op, new P5StringCompareBinder(runtime, operation));
        }

        protected override Expression ConvertBoolean(Subroutine sub, Opcode op)
        {
            return UnaryOperator<object>(
                sub, op, new P5BooleanBinder(runtime));
        }

        protected override Expression ScalarAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue)
        {
            return BinaryOperator<object>(
                sub, lvalue, rvalue,
                new P5ScalarAssignmentBinder(runtime));
        }

        protected override Expression ArrayAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression rvalue, bool common)
        {
            return BinaryOperator<object>(
                sub, lvalue, rvalue,
                new P5ArrayAssignmentBinder(runtime, cxt, common));
        }

        protected override Expression ArrayItem(Subroutine sub, Opcode.ContextValues cxt, Expression value, Expression index, bool create)
        {
            return BinaryOperator<object>(
                sub, value, index,
                new P5ArrayItemBinder(runtime, create));
        }

        protected override Expression ArrayItemAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression index, Expression rvalue)
        {
            var binder = new P5ArrayItemAssignmentBinder(runtime);
            var exps = new Expression[] { null, lvalue, index, rvalue };

            return Utils.GenerateCall(exps, binder);
        }

        protected override Expression HashItem(Subroutine sub, Opcode.ContextValues cxt, Expression value, Expression index, bool create)
        {
            return BinaryOperator<object>(
                sub, value, index,
                new P5HashItemBinder(runtime, create));
        }

        protected override Expression HashItemAssign(Subroutine sub, Opcode.ContextValues cxt, Expression lvalue, Expression index, Expression rvalue)
        {
            var binder = new P5HashItemAssignmentBinder(runtime);
            var exps = new Expression[] { null, lvalue, index, rvalue };

            return Utils.GenerateCall(exps, binder);
        }

        protected override Expression Iterator(Subroutine sub, Expression value)
        {
            return UnaryOperator<IEnumerator>(
                value, new P5IteratorBinder(runtime));
        }

        protected override Expression Defined(Subroutine sub, Opcode op)
        {
            return UnaryOperator<object>(
                sub, op, new P5DefinedBinder(runtime));
        }

        protected override void DefinePackage(string pack)
        {
            runtime.SymbolTable.GetPackage(runtime, pack, true);
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
