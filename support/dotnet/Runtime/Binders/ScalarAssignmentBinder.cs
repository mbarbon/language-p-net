using org.mbarbon.p.values;

using ICollection = System.Collections.ICollection;
using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5ScalarAssignmentBinder : DynamicMetaObjectBinder
    {
        public P5ScalarAssignmentBinder(Runtime _runtime)
        {
            runtime = _runtime;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            DynamicMetaObject arg = args[0];

            if (Utils.IsAny(target) && Utils.IsAny(arg))
                return BindIAny(target, arg);
            if (Utils.IsScalar(target))
                return BindScalar(target, arg);
            if (Utils.IsList(arg))
                return BindList(target, arg);

            return BindFallback(target, arg);
        }

        private Expression AsScalar(DynamicMetaObject arg)
        {
            if (Utils.IsList(arg))
                return Expression.Convert(
                    Expression.Property(
                        Expression.Convert(arg.Expression, typeof(ICollection)),
                        typeof(ICollection).GetProperty("Count")),
                    typeof(object));

            return Utils.CastObject(arg);
        }

        private DynamicMetaObject BindIAny(DynamicMetaObject target, DynamicMetaObject arg)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Call(
                        Utils.CastRuntime(target),
                        target.RuntimeType.GetMethod("Assign"),
                        Expression.Constant(runtime),
                        Utils.CastRuntime(arg)),
                    target.RuntimeType),
                Utils.RestrictToRuntimeType(target, arg));
        }

        private DynamicMetaObject BindScalar(DynamicMetaObject target, DynamicMetaObject arg)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Call(
                        Utils.CastRuntime(target),
                        target.RuntimeType.GetMethod("AssignObject"),
                        Expression.Constant(runtime),
                        AsScalar(arg)),
                    target.RuntimeType),
                Utils.RestrictToRuntimeType(target));
        }

        private DynamicMetaObject BindList(DynamicMetaObject target, DynamicMetaObject arg)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(arg.Expression, typeof(ICollection)),
                        typeof(ICollection).GetProperty("Count")),
                    typeof(object)),
                Utils.RestrictToRuntimeType(arg));
        }

        private DynamicMetaObject BindFallback(DynamicMetaObject target, DynamicMetaObject arg)
        {
            return new DynamicMetaObject(
                arg.Expression,
                Utils.RestrictToRuntimeType(arg));
        }

        private Runtime runtime;
    }
}
