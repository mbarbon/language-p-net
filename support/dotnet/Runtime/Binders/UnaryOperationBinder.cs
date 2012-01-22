using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5UnaryOperationBinder : UnaryOperationBinder
    {
        public P5UnaryOperationBinder(Runtime _runtime, ExpressionType op) :
            base(op)
        {
            runtime = _runtime;
        }

        public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            switch (Operation)
            {
            case ExpressionType.Not:
            case ExpressionType.Negate:
            case ExpressionType.OnesComplement:
                if (Utils.IsAny(target))
                    return BindAny(target, errorSuggestion);
                if (Utils.IsString(target))
                    return BindString(target, errorSuggestion);
                if (Utils.IsInteger(target))
                    return BindInteger(target, errorSuggestion);
                if (Utils.IsFloat(target))
                    return BindFloat(target, errorSuggestion);
                if (Utils.IsBoolean(target))
                {
                    if (Operation == ExpressionType.Not)
                        return BindBooleanNot(target, errorSuggestion);
                    else
                        return BindInteger(target, errorSuggestion);
                }

                throw new System.Exception("Unable to bind " + target.RuntimeType);
            default:
                throw new System.Exception("Implement operation binding for " + Operation);
            }
        }

        private DynamicMetaObject BindAny(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            string default_conversion;
            Expression scalar_expression = null;

            switch (Operation)
            {
            case ExpressionType.OnesComplement:
                default_conversion = "AsInteger";
                scalar_expression = Expression.Call(
                    typeof(Builtins).GetMethod("BitNot"),
                    Expression.Constant(runtime),
                    Utils.CastScalar(target));
                break;
            case ExpressionType.Negate:
                default_conversion = "AsInteger";
                scalar_expression = Expression.Call(
                    typeof(Builtins).GetMethod("Negate"),
                    Expression.Constant(runtime),
                    Utils.CastScalar(target));
                break;
            case ExpressionType.Not:
                default_conversion = "AsBoolean";
                break;
            default:
                throw new System.Exception("Implement me");
            }

            if (Utils.IsScalar(target) && scalar_expression != null)
                return new DynamicMetaObject(
                    scalar_expression,
                    Utils.RestrictToScalar(target));
            else if (Utils.IsAny(target))
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.MakeUnary(
                            Operation,
                            Expression.Call(
                                Utils.CastAny(target),
                                typeof(IP5Any).GetMethod(default_conversion),
                                Expression.Constant(runtime)),
                            null),
                        typeof(object)),
                    Utils.RestrictToRuntimeType(target));

            throw new System.Exception("Unable to bind " + target.RuntimeType);
        }

        private DynamicMetaObject BindString(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            switch (Operation)
            {
            case ExpressionType.OnesComplement:
                return new DynamicMetaObject(
                    Expression.Call(
                        typeof(Builtins).GetMethod("BitNotString"),
                        Expression.Constant(runtime),
                        target.Expression),
                    Utils.RestrictToString(target));
            case ExpressionType.Negate:
                return new DynamicMetaObject(
                    Expression.Add(
                        Expression.Constant("-"),
                        target.Expression),
                    Utils.RestrictToString(target));
            case ExpressionType.Not:
            {
                var boolean = BinderUtils.ConvertBoolean(runtime, target);

                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.Not(boolean.Expression),
                        typeof(object)),
                    boolean.Restrictions);
            }
            default:
                throw new System.Exception("Implement me");
            }
        }

        private DynamicMetaObject BindInteger(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            switch (Operation)
            {
            case ExpressionType.OnesComplement:
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.OnesComplement(
                            Expression.Convert(target.Expression, typeof(int))),
                        typeof(object)),
                    Utils.RestrictToInteger(target));
            case ExpressionType.Negate:
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.Negate(
                            Expression.Convert(target.Expression, typeof(int))),
                        typeof(object)),
                    Utils.RestrictToInteger(target));
            case ExpressionType.Not:
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.Equal(
                            Expression.Convert(target.Expression, typeof(int)),
                            Expression.Constant(0)),
                        typeof(object)),
                    Utils.RestrictToInteger(target));
            default:
                throw new System.Exception("Implement me");
            }
        }

        private DynamicMetaObject BindFloat(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            switch (Operation)
            {
            case ExpressionType.OnesComplement:
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.OnesComplement(
                            Expression.Convert(target.Expression, typeof(int))),
                        typeof(object)),
                    Utils.RestrictToFloat(target));
            case ExpressionType.Negate:
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.Negate(
                            Expression.Convert(target.Expression, typeof(double))),
                        typeof(object)),
                    Utils.RestrictToFloat(target));
            case ExpressionType.Not:
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.Equal(
                            Expression.Convert(target.Expression, typeof(double)),
                            Expression.Constant(0.0)),
                        typeof(object)),
                    Utils.RestrictToFloat(target));
            default:
                throw new System.Exception("Implement me");
            }
        }

        private DynamicMetaObject BindBooleanNot(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Not(
                        Expression.Convert(target.Expression, typeof(bool))),
                    typeof(object)),
                Utils.RestrictToRuntimeType(target));
        }

        private Runtime runtime;
    }
}
