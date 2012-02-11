using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5StringOperationBinder : P5BinaryOperationBinder
    {
        public P5StringOperationBinder(ExpressionType op, Runtime _runtime) :
            base(op, _runtime)
        {
            runtime = _runtime;
        }

        protected override OverloadOperation OverloadOp()
        {
            switch (Operation)
            {
            case ExpressionType.Add:
                return OverloadOperation.CONCATENATE;
            case ExpressionType.AddAssign:
                return OverloadOperation.CONCATENATE_ASSIGN;
            default:
                throw new System.Exception("Unhandled overloaded operation");
            }
        }

        protected override Expression BaseOperation(DynamicMetaObject target, DynamicMetaObject arg)
        {
            bool is_assign = IsAssign();

            if (!Utils.IsAny(target))
            {
                Expression left = BinderUtils.ConvertString(runtime, target).Expression;
                Expression right = BinderUtils.ConvertString(runtime, arg).Expression;
                Expression exp = Expression.Call(
                    typeof(string).GetMethod("Concat", new System.Type[] { typeof(string), typeof(string) }),
                    left, right);

                if (is_assign)
                    exp = Expression.Assign(
                        left, exp);

                return Expression.Convert(exp, typeof(object));
            }
            else
            {
                string op_method = "ConcatenateScalarObject";

                if (is_assign)
                    op_method += "Assign";

                var method = typeof(Builtins).GetMethod(op_method);

                return Expression.Call(
                    method,
                    Expression.Constant(runtime),
                    AsScalarOrRuntime(target),
                    AsScalarOrObject(arg));
            }
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            switch (Operation)
            {
            case ExpressionType.Add:
            case ExpressionType.AddAssign:
                return BindOp(target, arg, errorSuggestion);
            default:
                throw new System.Exception("Unhandled operation value");
            }
        }

        private Runtime runtime;
    }
}
