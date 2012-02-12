using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5BinaryOperationBinder : BinaryOperationBinder
    {
        public P5BinaryOperationBinder(Runtime runtime, ExpressionType op) :
            base(op)
        {
            Runtime = runtime;
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            switch (Operation)
            {
            case ExpressionType.Or:
            case ExpressionType.OrAssign:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.And:
            case ExpressionType.AndAssign:
            case ExpressionType.Add:
            case ExpressionType.AddAssign:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractAssign:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.Divide:
            case ExpressionType.DivideAssign:
            case ExpressionType.LeftShift:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.RightShift:
            case ExpressionType.RightShiftAssign:
                return BindOp(target, arg, errorSuggestion);
            default:
                throw new System.Exception("Unhandled operation value");
            }
        }

        private Expression CallOverload(DynamicMetaObject target, DynamicMetaObject arg, OverloadOperation op, Expression fallback)
        {
            return Expression.Coalesce(
                Expression.Call(
                    typeof(Builtins).GetMethod("CallOverload"),
                    Expression.Constant(Runtime),
                    Expression.Constant(op),
                    AsScalarOrObject(target),
                    arg.Expression),
                fallback);
        }

        private Expression CallOverloadInverted(DynamicMetaObject target, DynamicMetaObject arg, OverloadOperation op, Expression fallback)
        {
            return Expression.Coalesce(
                Expression.Call(
                    typeof(Builtins).GetMethod("CallOverloadInverted"),
                    Expression.Constant(Runtime),
                    Expression.Constant(op),
                    target.Expression,
                    AsScalarOrObject(arg)),
                fallback);
        }

        private bool IsBitOperation()
        {
            switch (Operation)
            {
            case ExpressionType.LeftShift:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.RightShift:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.And:
            case ExpressionType.AndAssign:
            case ExpressionType.Or:
            case ExpressionType.OrAssign:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.ExclusiveOrAssign:
                return true;
            default:
                return false;
            }
        }

        protected virtual OverloadOperation OverloadOp()
        {
            switch (Operation)
            {
            case ExpressionType.Add:
                return OverloadOperation.ADD;
            case ExpressionType.AddAssign:
                return OverloadOperation.ADD_ASSIGN;
            case ExpressionType.Subtract:
                return OverloadOperation.SUBTRACT;
            case ExpressionType.SubtractAssign:
                return OverloadOperation.SUBTRACT_ASSIGN;
            case ExpressionType.Multiply:
                return OverloadOperation.MULTIPLY;
            case ExpressionType.MultiplyAssign:
                return OverloadOperation.MULTIPLY_ASSIGN;
            case ExpressionType.Divide:
                return OverloadOperation.DIVIDE;
            case ExpressionType.DivideAssign:
                return OverloadOperation.DIVIDE_ASSIGN;
            case ExpressionType.LeftShift:
                return OverloadOperation.SHIFT_LEFT;
            case ExpressionType.LeftShiftAssign:
                return OverloadOperation.SHIFT_LEFT_ASSIGN;
            case ExpressionType.RightShift:
                return OverloadOperation.SHIFT_RIGHT;
            case ExpressionType.RightShiftAssign:
                return OverloadOperation.SHIFT_RIGHT_ASSIGN;
            case ExpressionType.And:
                return OverloadOperation.AND;
            case ExpressionType.AndAssign:
                return OverloadOperation.AND_ASSIGN;
            case ExpressionType.Or:
                return OverloadOperation.OR;
            case ExpressionType.OrAssign:
                return OverloadOperation.OR_ASSIGN;
            case ExpressionType.ExclusiveOr:
                return OverloadOperation.XOR;
            case ExpressionType.ExclusiveOrAssign:
                return OverloadOperation.XOR_ASSIGN;
            default:
                throw new System.Exception("Unhandled overloaded operation");
            }
        }

        protected bool IsAssign()
        {
            switch (Operation)
            {
            case ExpressionType.AddAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.AndAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.ExclusiveOrAssign:
                return true;
            default:
                return false;
            }
        }

        private string OpName()
        {
            switch (Operation)
            {
            case ExpressionType.Add:
            case ExpressionType.AddAssign:
                return "Add";
            case ExpressionType.Subtract:
            case ExpressionType.SubtractAssign:
                return "Subtract";
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyAssign:
                return "Multiply";
            case ExpressionType.Divide:
            case ExpressionType.DivideAssign:
                return "Divide";
            case ExpressionType.LeftShift:
            case ExpressionType.LeftShiftAssign:
                return "LeftShift";
            case ExpressionType.RightShift:
            case ExpressionType.RightShiftAssign:
                return "RightShift";
            case ExpressionType.And:
            case ExpressionType.AndAssign:
                return "BitAnd";
            case ExpressionType.Or:
            case ExpressionType.OrAssign:
                return "BitOr";
            case ExpressionType.ExclusiveOr:
            case ExpressionType.ExclusiveOrAssign:
                return "BitXor";
            default:
                throw new System.Exception("Unhandled overloaded operation");
            }
        }

        private string TypeName(DynamicMetaObject arg)
        {
            if (Utils.IsInteger(arg))
                return "Integer";
            if (Utils.IsFloat(arg))
                return "Float";
            if (Utils.IsString(arg))
                return "String";
            if (Utils.IsAny(arg))
                return "Scalar";

            throw new System.Exception("Unhandled type in arithmetic operation " + arg.RuntimeType);
        }

        protected Expression AsScalarOrRuntime(DynamicMetaObject arg)
        {
            if (Utils.IsAny(arg) && !Utils.IsScalar(arg))
                return Expression.Call(
                    Utils.CastAny(arg),
                    typeof(IP5Any).GetMethod("AsScalar"),
                    Expression.Constant(Runtime));
            if (Utils.IsScalar(arg))
                return Utils.CastScalar(arg);

            return Utils.CastRuntime(arg);
        }

        protected Expression AsScalarOrObject(DynamicMetaObject arg)
        {
            if (Utils.IsAny(arg) && !Utils.IsScalar(arg))
                return Expression.Call(
                    Utils.CastAny(arg),
                    typeof(IP5Any).GetMethod("AsScalar"),
                    Expression.Constant(Runtime));
            if (Utils.IsScalar(arg))
                return Utils.CastScalar(arg);

            return Expression.Convert(arg.Expression, typeof(object));
        }

        private Expression AsFloat(DynamicMetaObject arg)
        {
            Expression scalar = null;

            if (Utils.IsInteger(arg))
                return Expression.Convert(
                    Utils.CastInteger(arg), typeof(double));
            else if (Utils.IsAny(arg) && !Utils.IsScalar(arg))
                scalar = Expression.Call(
                    Utils.CastAny(arg),
                    typeof(IP5Any).GetMethod("AsScalar"),
                    Expression.Constant(Runtime));
            else if (Utils.IsScalar(arg))
                scalar = Utils.CastRuntime(arg);
            else if (Utils.IsFloat(arg))
                return Utils.CastFloat(arg);
            else
                throw new System.Exception("Unhandled type " + arg.RuntimeType);

            return Expression.Call(
                scalar,
                typeof(P5Scalar).GetMethod("AsFloat"),
                Expression.Constant(Runtime));
        }

        protected virtual Expression BaseOperation(DynamicMetaObject target, DynamicMetaObject arg)
        {
            bool is_assign = IsAssign();
            string op_method = OpName() + TypeName(target) + TypeName(arg) +
                (is_assign ? "Assign" : "");
            var method = typeof(Builtins).GetMethod(op_method);

            if (method == null)
            {
                Expression left, right;

                if (IsBitOperation())
                {
                    left = BinderUtils.ConvertInteger(Runtime, target).Expression;
                    right = BinderUtils.ConvertInteger(Runtime, arg).Expression;
                }
                else if (target.RuntimeType != arg.RuntimeType)
                {
                    left = AsFloat(target);
                    right = AsFloat(arg);
                }
                else
                {
                    left = Utils.CastRuntime(target);
                    right = Utils.CastRuntime(arg);
                }

                return Expression.Convert(
                    Expression.MakeBinary(
                        Operation,
                        left,
                        right),
                    typeof(object));
            }
            else
            {
                return Expression.Call(
                    method,
                    Expression.Constant(Runtime),
                    AsScalarOrRuntime(target),
                    AsScalarOrRuntime(arg));
            }
        }

        protected DynamicMetaObject BindOp(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            OverloadOperation ovl_op = OverloadOp();
            bool is_assign = IsAssign();

            // if left/right is any, call AsScalar
            // scalar <op> object -> overload left, then right,
            //                       then call builtin
            // object <op> scalar -> overload right, then call builtin
            // object <op> object -> call builtin

            if (Utils.IsAny(target))
            {
                return new DynamicMetaObject(
                    CallOverload(
                        target,
                        arg,
                        ovl_op,
                        BaseOperation(target, arg)),
                    Utils.RestrictToRuntimeType(target)
                        .Merge(Utils.RestrictToRuntimeType(arg)));
            }
            else if (Utils.IsAny(arg))
            {
                if (is_assign)
                    throw new System.Exception("Implement me");

                return new DynamicMetaObject(
                    CallOverloadInverted(
                        target,
                        arg,
                        ovl_op,
                        BaseOperation(target, arg)),
                    Utils.RestrictToRuntimeType(target)
                        .Merge(Utils.RestrictToRuntimeType(arg)));
            }
            else
            {
                if (is_assign)
                    throw new System.Exception("Implement me");

                return new DynamicMetaObject(
                    BaseOperation(target, arg),
                    Utils.RestrictToAny(target, arg));
            }
        }

        private Runtime Runtime;
    }
}
