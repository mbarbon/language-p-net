using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5DefinedBinder : DynamicMetaObjectBinder
    {
        public P5DefinedBinder(Runtime _runtime)
        {
            runtime = _runtime;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (Utils.IsValue(target))
                return BindValue(target);

            if (Utils.IsInteger(target) ||
                Utils.IsFloat(target) ||
                Utils.IsBoolean(target))
                return new DynamicMetaObject(
                    Expression.Convert(
                        Expression.Constant(true), typeof(object)),
                    Utils.RestrictToRuntimeType(target));

            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.NotEqual(
                        Expression.Constant(null, typeof(object)),
                        target.Expression),
                    typeof(object)),
                BindingRestrictions.Empty);
        }

        private DynamicMetaObject BindValue(DynamicMetaObject target)
        {
            return new DynamicMetaObject(
                Expression.Convert(
                    Expression.Call(
                        Utils.CastRuntime(target),
                        target.RuntimeType.GetMethod("IsDefined"),
                        Expression.Constant(runtime)),
                    typeof(object)),
                Utils.RestrictToRuntimeType(target));
        }

        Runtime runtime;
    }
}
