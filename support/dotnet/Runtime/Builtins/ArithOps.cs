using org.mbarbon.p.values;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        // Addition

        public static object AddScalarScalarAssign(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            left.AssignObject(runtime, AddScalarScalar(runtime, left, right));

            return left;
        }

        public static object AddScalarFloatAssign(Runtime runtime, P5Scalar left, double right)
        {
            left.AssignObject(runtime, left.AsFloat(runtime) + right);

            return left;
        }

        public static object AddScalarIntegerAssign(Runtime runtime, P5Scalar left, int right)
        {
            left.AssignObject(runtime, AddScalarInteger(runtime, left, right));

            return left;
        }

        public static object AddScalarInteger(Runtime runtime, P5Scalar left, int right)
        {
            if (left.IsInteger(runtime))
                return AddIntegerInteger(runtime, left.AsInteger(runtime), right);
            else if (left.IsFloat(runtime))
                return left.AsFloat(runtime) + right;

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object AddIntegerScalar(Runtime runtime, int left, P5Scalar right)
        {
            if (right.IsInteger(runtime))
                return AddIntegerInteger(runtime, left, right.AsInteger(runtime));
            else if (right.IsFloat(runtime))
                return left + right.AsFloat(runtime);

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object AddIntegerInteger(Runtime runtime, int left, int right)
        {
            // TODO handle integer -> float promotion
            return left + right;
        }

        public static object AddScalarScalar(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            // TODO handle integer addition and integer -> float promotion
            return left.AsFloat(runtime) + right.AsFloat(runtime);
        }

        // Subtraction

        public static object SubtractScalarScalarAssign(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            left.AssignObject(runtime, SubtractScalarScalar(runtime, left, right));

            return left;
        }

        public static object SubtractScalarFloatAssign(Runtime runtime, P5Scalar left, double right)
        {
            left.AssignObject(runtime, left.AsFloat(runtime) - right);

            return left;
        }

        public static object SubtractScalarIntegerAssign(Runtime runtime, P5Scalar left, int right)
        {
            left.AssignObject(runtime, SubtractScalarInteger(runtime, left, right));

            return left;
        }

        public static object SubtractScalarInteger(Runtime runtime, P5Scalar left, int right)
        {
            if (left.IsInteger(runtime))
                return SubtractIntegerInteger(runtime, left.AsInteger(runtime), right);
            else if (left.IsFloat(runtime))
                return left.AsFloat(runtime) - right;

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object SubtractIntegerScalar(Runtime runtime, int left, P5Scalar right)
        {
            if (right.IsInteger(runtime))
                return SubtractIntegerInteger(runtime, left, right.AsInteger(runtime));
            else if (right.IsFloat(runtime))
                return left - right.AsFloat(runtime);

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object SubtractIntegerInteger(Runtime runtime, int left, int right)
        {
            // TODO handle integer -> float promotion
            return left - right;
        }

        public static object SubtractScalarScalar(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            // TODO handle integer addition and integer -> float promotion
            return left.AsFloat(runtime) - right.AsFloat(runtime);
        }

        // Multiplication

        public static object MultiplyScalarScalarAssign(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            left.AssignObject(runtime, MultiplyScalarScalar(runtime, left, right));

            return left;
        }

        public static object MultiplyScalarFloatAssign(Runtime runtime, P5Scalar left, double right)
        {
            left.AssignObject(runtime, left.AsFloat(runtime) * right);

            return left;
        }

        public static object MultiplyScalarIntegerAssign(Runtime runtime, P5Scalar left, int right)
        {
            left.AssignObject(runtime, MultiplyScalarInteger(runtime, left, right));

            return left;
        }

        public static object MultiplyScalarInteger(Runtime runtime, P5Scalar left, int right)
        {
            if (left.IsInteger(runtime))
                return MultiplyIntegerInteger(runtime, left.AsInteger(runtime), right);
            else if (left.IsFloat(runtime))
                return left.AsFloat(runtime) * right;

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object MultiplyIntegerScalar(Runtime runtime, int left, P5Scalar right)
        {
            if (right.IsInteger(runtime))
                return MultiplyIntegerInteger(runtime, left, right.AsInteger(runtime));
            else if (right.IsFloat(runtime))
                return left * right.AsFloat(runtime);

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object MultiplyIntegerInteger(Runtime runtime, int left, int right)
        {
            // TODO handle integer -> float promotion
            return left * right;
        }

        public static object MultiplyScalarScalar(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            // TODO handle integer addition and integer -> float promotion
            return left.AsFloat(runtime) * right.AsFloat(runtime);
        }

        // Division

        public static object DivideScalarScalarAssign(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            left.AssignObject(runtime, DivideScalarScalar(runtime, left, right));

            return left;
        }

        public static object DivideScalarFloatAssign(Runtime runtime, P5Scalar left, double right)
        {
            left.AssignObject(runtime, left.AsFloat(runtime) / right);

            return left;
        }

        public static object DivideScalarIntegerAssign(Runtime runtime, P5Scalar left, int right)
        {
            left.AssignObject(runtime, DivideScalarInteger(runtime, left, right));

            return left;
        }

        public static object DivideScalarInteger(Runtime runtime, P5Scalar left, int right)
        {
            if (left.IsInteger(runtime))
                return DivideIntegerInteger(runtime, left.AsInteger(runtime), right);
            else if (left.IsFloat(runtime))
                return left.AsFloat(runtime) / right;

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object DivideIntegerScalar(Runtime runtime, int left, P5Scalar right)
        {
            if (right.IsInteger(runtime))
                return DivideIntegerInteger(runtime, left, right.AsInteger(runtime));
            else if (right.IsFloat(runtime))
                return left / right.AsFloat(runtime);

            throw new System.Exception("Handle string -> number conversion");
        }

        public static object DivideIntegerInteger(Runtime runtime, int left, int right)
        {
            // TODO handle integer -> float promotion
            return left / (double) right;
        }

        public static object DivideScalarScalar(Runtime runtime, P5Scalar left, P5Scalar right)
        {
            // TODO handle integer addition and integer -> float promotion
            return left.AsFloat(runtime) / right.AsFloat(runtime);
        }
    }
}
