using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5UnaryIncrementBinder : DynamicMetaObjectBinder
    {
        public P5UnaryIncrementBinder(Runtime _runtime, ExpressionType _operation)
        {
            runtime = _runtime;
            operation = _operation;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (Utils.IsScalar(target))
            {
                string method;

                switch (operation)
                {
                case ExpressionType.PreIncrementAssign:
                    method = "PreIncrement";
                    break;
                case ExpressionType.PreDecrementAssign:
                    method = "PreDecrement";
                    break;
                case ExpressionType.PostIncrementAssign:
                    method = "PostIncrement";
                    break;
                case ExpressionType.PostDecrementAssign:
                    method = "PostDecrement";
                    break;
                default:
                    throw new System.Exception("Invalid operation");
                }

                return new DynamicMetaObject(
                    Expression.Call(
                        Utils.CastScalar(target),
                        typeof(P5Scalar).GetMethod(method),
                        Expression.Constant(runtime)),
                    Utils.RestrictToScalar(target));
            }

            // TODO integer -> float promotion
            Expression expr = null;

            switch (operation)
            {
            case ExpressionType.PreIncrementAssign:
                expr = MakePre(target, ExpressionType.Increment);
                break;
            case ExpressionType.PreDecrementAssign:
                expr = MakePre(target, ExpressionType.Decrement);
                break;
            case ExpressionType.PostIncrementAssign:
                expr = MakePre(target, ExpressionType.Increment);
                break;
            case ExpressionType.PostDecrementAssign:
                expr = MakePost(target, ExpressionType.Decrement);
                break;
            default:
                throw new System.Exception("Invalid operation");
            }

            return new DynamicMetaObject(
                expr,
                Utils.RestrictToRuntimeType(target));
        }

        private Expression MakePre(DynamicMetaObject target, ExpressionType op)
        {
            System.Console.WriteLine(target.Expression.GetType());
            return Expression.Assign(
                target.Expression,
                Expression.Convert(Expression.Constant(1), typeof(object)));

                // Expression.Convert(
                //     Expression.MakeUnary(
                //         op,
                //         Utils.CastRuntime(target),
                //         target.RuntimeType),
                //     typeof(object)));
        }

        private Expression MakePost(DynamicMetaObject target, ExpressionType op)
        {
            var temp = Expression.Parameter(typeof(object));

            return Expression.Block(
                new ParameterExpression[] { temp },
                Expression.Assign(temp, target.Expression),
                Expression.Assign(
                    target.Expression,
                    Expression.MakeUnary(
                        op,
                        Utils.CastRuntime(target),
                        typeof(object))),
                temp);
        }

        private Runtime runtime;
        private ExpressionType operation;
    }
}
