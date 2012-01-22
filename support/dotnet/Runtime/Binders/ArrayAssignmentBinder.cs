using org.mbarbon.p.values;

using System.Dynamic;
using System.Collections.Generic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5ArrayAssignmentBinder : DynamicMetaObjectBinder
    {
        public P5ArrayAssignmentBinder(Runtime runtime, Opcode.ContextValues cxt, bool common)
        {
            Runtime = runtime;
            Context = cxt;
            Common = common;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            DynamicMetaObject arg = args[0];

            if (arg.RuntimeType == typeof(P5Range))
                return BindRange(target, arg);

            return BindFallback(target, arg);
        }

        private Expression ContextExpression()
        {
            if (Context == Opcode.ContextValues.CALLER)
                return Expression.Call(
                    Expression.Constant(Runtime),
                    typeof(Runtime).GetMethod("CurrentContext"));
            else
                return Expression.Constant(Context);
        }

        private DynamicMetaObject BindRange(DynamicMetaObject target, DynamicMetaObject arg)
        {
            var lvalue = Expression.Parameter(target.RuntimeType);
            var rvalue = Expression.Parameter(arg.RuntimeType);
            var assignment = Expression.Call(
                lvalue,
                target.RuntimeType.GetMethod("AssignIterator"),
                Expression.Constant(Runtime),
                Expression.Call(
                    rvalue,
                    typeof(IP5Enumerable).GetMethod("GetEnumerator"),
                    Expression.Constant(Runtime)));
            var result = Expression.Condition(
                Expression.Equal(
                    ContextExpression(),
                    Expression.Constant(Opcode.ContextValues.SCALAR)),
                Expression.New(
                    typeof(P5Scalar).GetConstructor(new System.Type[] { typeof(Runtime), typeof(int) }),
                    Expression.Constant(Runtime),
                    Expression.Call(
                        rvalue,
                        typeof(P5Range).GetMethod("GetCount"))),
                lvalue,
                typeof(IP5Any));

            return new DynamicMetaObject(
                Expression.Block(
                    typeof(IP5Any),
                    new ParameterExpression[] { lvalue, rvalue },
                    new Expression[] {
                        Expression.Assign(lvalue, Utils.CastRuntime(target)),
                        Expression.Assign(rvalue, Utils.CastRuntime(arg)),
                        assignment,
                        result } ),
                Utils.RestrictToRuntimeType(arg, target));
        }

        private DynamicMetaObject BindFallback(DynamicMetaObject target, DynamicMetaObject arg)
        {
            Expression rvalue;

            if (Common)
            {
                if (Utils.IsAny(arg))
                    rvalue = Expression.Call(
                        Utils.CastAny(arg),
                        typeof(IP5Any).GetMethod("Clone"),
                        Expression.Constant(Runtime),
                        Expression.Constant(1));
                else
                    // FIXME only handles List<object>
                    rvalue = Expression.Call(
                        typeof(Builtins).GetMethod("CloneList"),
                        Expression.Constant(Runtime),
                        Expression.Convert(arg.Expression, typeof(List<object>)),
                        Expression.Constant(1));
            }
            else
                rvalue = arg.Expression;

            Expression lvalue, assignment;
            ParameterExpression collection = null;

            if (Context == Opcode.ContextValues.VOID)
                lvalue = Utils.CastRuntime(target);
            else
                lvalue = collection = Expression.Parameter(target.RuntimeType);

            if (Utils.IsAny(target))
                assignment = Expression.Call(
                    lvalue,
                    target.RuntimeType.GetMethod("AssignArray"),
                    Expression.Constant(Runtime),
                    // FIXME does not handle assigning a List to an array
                    rvalue);
            else
                // FIXME only handles List<object>
                assignment = Expression.Call(
                    typeof(Builtins).GetMethod("AssignArrayList"),
                    Expression.Constant(Runtime),
                    lvalue,
                    rvalue);

            if (Context == Opcode.ContextValues.VOID)
                return new DynamicMetaObject(
                    Expression.Block(
                        assignment,
                        Expression.Constant(null, typeof(IP5Any))),
                    Utils.RestrictToRuntimeType(arg, target));

            var assign_result = Expression.Parameter(typeof(object));
            var result = Expression.Condition(
                Expression.Equal(
                    ContextExpression(),
                    Expression.Constant(Opcode.ContextValues.SCALAR)),
                assign_result,
                lvalue,
                typeof(object));
            var expression = Expression.Block(
                typeof(object),
                new ParameterExpression[] { assign_result, collection },
                new Expression[] {
                    Expression.Assign(lvalue, Utils.CastRuntime(target)),
                    Expression.Assign(assign_result, assignment),
                    result } );

            return new DynamicMetaObject(
                expression,
                Utils.RestrictToRuntimeType(arg, target));
        }

        private Runtime Runtime;
        private Opcode.ContextValues Context;
        private bool Common;
    }
}
