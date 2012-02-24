using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5BooleanBinder : DynamicMetaObjectBinder
    {
        public P5BooleanBinder(Runtime _runtime)
        {
            runtime = _runtime;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (Utils.IsAny(target))
                return BindAny(target);
            else if (Utils.IsInteger(target))
                return BindInteger(target);
            else if (Utils.IsFloat(target))
                return BindFloat(target);
            else if (Utils.IsBoolean(target))
                return BindBoolean(target);

            throw new System.Exception("Unhandled type in boolean conversion " + target.RuntimeType);
        }

        private DynamicMetaObject BindAny(DynamicMetaObject target)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Call(
                        Utils.CastAny(target),
                        typeof(IP5Any).GetMethod("AsBoolean"),
                        Expression.Constant(runtime)),
                    typeof(object)),
                Utils.RestrictToAny(target));
        }

        private DynamicMetaObject BindInteger(DynamicMetaObject target)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.NotEqual(
                        Utils.CastRuntime(target),
                        Expression.Constant(0)),
                    typeof(object)),
                Utils.RestrictToInteger(target));
        }

        private DynamicMetaObject BindFloat(DynamicMetaObject target)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.NotEqual(
                        Utils.CastRuntime(target),
                        Expression.Constant(0.0)),
                    typeof(object)),
                Utils.RestrictToFloat(target));
        }

        private DynamicMetaObject BindBoolean(DynamicMetaObject target)
        {
            return new DynamicMetaObject(
                target.Expression,
                Utils.RestrictToBoolean(target));
        }

        Runtime runtime;
    }
}
