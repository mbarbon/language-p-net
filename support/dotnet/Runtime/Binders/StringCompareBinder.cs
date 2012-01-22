using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5StringCompareBinder : BinaryOperationBinder
    {
        public P5StringCompareBinder(Runtime _runtime, ExpressionType _op) :
            base(_op)
        {
            runtime = _runtime;
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            switch (Operation)
            {
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                return BindRelOp(target, arg, errorSuggestion);
            default:
                throw new System.Exception("Unhandled operation value");
            }
        }

        private DynamicMetaObject BindRelOp(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            var left = BinderUtils.ConvertString(runtime, target);
            var right = BinderUtils.ConvertString(runtime, arg);

            // TODO handle overload for scalar types

            return new DynamicMetaObject(
                Expression.Convert(
                    CompareStrings(left.Expression, right.Expression),
                    typeof(object)),
                left.Restrictions.Merge(right.Restrictions));
        }

        private Expression CompareStrings(Expression l, Expression r)
        {
            return Expression.MakeBinary(
                Operation,
                Expression.Call(
                    typeof(string).GetMethod("Compare", new[] { typeof(string), typeof(string) }),
                    l, r),
                Expression.Constant(0));
        }

        private Runtime runtime;
    }
}
