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
                return BindBitOp(target, arg, errorSuggestion);
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

        private DynamicMetaObject BindBitOp(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            string method_name;
            bool is_assign;

            switch (Operation)
            {
            case ExpressionType.Or:
                method_name = "BitOr";
                is_assign = false;
                break;
            case ExpressionType.OrAssign:
                method_name = "BitOrAssign";
                is_assign = true;
                break;
            case ExpressionType.ExclusiveOr:
                method_name = "BitXor";
                is_assign = false;
                break;
            case ExpressionType.ExclusiveOrAssign:
                method_name = "BitXorAssign";
                is_assign = true;
                break;
            case ExpressionType.And:
                method_name = "BitAnd";
                is_assign = false;
                break;
            case ExpressionType.AndAssign:
                method_name = "BitAndAssign";
                is_assign = true;
                break;
            default:
                throw new System.Exception("Unhandled operation value");
            }

            if (Utils.IsScalar(target) && Utils.IsScalar(arg))
            {
                Expression expression;

                if (is_assign)
                    expression = Expression.Call(
                        typeof(Builtins).GetMethod(method_name),
                        Expression.Constant(Runtime),
                        Utils.CastScalar(target),
                        Utils.CastScalar(arg));
                else
                    expression = Expression.Call(
                        typeof(Builtins).GetMethod(method_name),
                        Expression.Constant(Runtime),
                        Expression.New(
                            typeof(P5Scalar).GetConstructor(new[] { typeof(IP5ScalarBody) }),
                            Expression.Constant(null, typeof(IP5ScalarBody))),
                        Utils.CastScalar(target),
                        Utils.CastScalar(arg));

                return new DynamicMetaObject(
                    expression,
                    Utils.RestrictToScalar(arg, target));
            }
            else if (Utils.IsAny(target) && Utils.IsAny(arg))
            {
                var value = Expression.MakeBinary(
                    Operation,
                    Expression.Call(
                        Utils.CastAny(target),
                        typeof(IP5Any).GetMethod("AsInteger"),
                        Expression.Constant(Runtime)),
                    Expression.Call(
                        Utils.CastAny(arg),
                        typeof(IP5Any).GetMethod("AsInteger"),
                        Expression.Constant(Runtime)));
                Expression expression;

                if (is_assign)
                    expression = Expression.Call(
                        Utils.CastScalar(target),
                        typeof(IP5Any).GetMethod("Assign"),
                        value);
                else
                    expression = Expression.New(
                        typeof(P5Scalar).GetConstructor(new[] {typeof(Runtime), typeof(int)}),
                        Expression.Constant(Runtime),
                        value);

                return new DynamicMetaObject(
                    expression,
                    Utils.RestrictToRuntimeType(arg, target));
            }

            throw new System.Exception("Unable to bind " + target.RuntimeType + " " + arg.RuntimeType);
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

                if (target.RuntimeType != arg.RuntimeType)
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
