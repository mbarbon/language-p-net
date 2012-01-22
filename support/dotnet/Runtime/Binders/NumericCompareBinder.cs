using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5NumericCompareBinder : BinaryOperationBinder
    {
        public P5NumericCompareBinder(Runtime _runtime, ExpressionType _op) :
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
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
                return BindRelOp(target, arg, errorSuggestion);
            default:
                throw new System.Exception("Unhandled operation value");
            }
        }

        private Expression AsNumber(DynamicMetaObject arg)
        {
            Expression scalar = null;

            if (Utils.IsInteger(arg))
                return Utils.CastInteger(arg);
            else if (Utils.IsAny(arg) && !Utils.IsScalar(arg))
                scalar = Expression.Call(
                    Utils.CastAny(arg),
                    typeof(IP5Any).GetMethod("AsScalar"),
                    Expression.Constant(runtime));
            else if (Utils.IsScalar(arg))
                scalar = Utils.CastScalar(arg);
            else if (Utils.IsFloat(arg))
                return Utils.CastFloat(arg);
            else if (Utils.IsNull(arg))
                // TODO warn
                return Expression.Constant(0);
            else
                throw new System.Exception("Unhandled type " + arg.RuntimeType);

            return Expression.Call(
                scalar,
                typeof(P5Scalar).GetMethod("AsFloat"),
                Expression.Constant(runtime));
        }

        private DynamicMetaObject BindRelOp(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            var left = AsNumber(target);
            var right = AsNumber(arg);

            // TODO handle overload for scalar types

            if (target.RuntimeType != arg.RuntimeType ||
                (!Utils.IsInteger(target) && !Utils.IsFloat(target)))
            {
                if (Utils.IsInteger(target))
                    left = Expression.Convert(left, typeof(double));
                else if (Utils.IsNull(target))
                    // TODO warn
                    left = Expression.Constant(0.0);
                if (Utils.IsInteger(arg))
                    right = Expression.Convert(right, typeof(double));
                else if (Utils.IsNull(arg))
                    // TODO warn
                    right = Expression.Constant(0.0);
            }

            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.MakeBinary(
                        Operation,
                        left,
                        right),
                    typeof(object)),
                Utils.RestrictToRuntimeType(arg, target));
        }

        private Runtime runtime;
    }
}
