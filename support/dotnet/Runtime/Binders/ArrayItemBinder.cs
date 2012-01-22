using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5ArrayItemBinder : DynamicMetaObjectBinder
    {
        public P5ArrayItemBinder(Runtime _runtime, bool _create)
        {
            runtime = _runtime;
            create = _create;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (Utils.IsArray(target))
                return BindIP5Array(target, args[0]);

            return BindFallback(target, args[0]);
        }

        private DynamicMetaObject BindIP5Array(DynamicMetaObject target, DynamicMetaObject index)
        {
            var idx = BinderUtils.ConvertInteger(runtime, index);

            return new DynamicMetaObject(
                Expression.Call(
                    Utils.CastRuntime(target),
                    typeof(IP5Array).GetMethod("GetItemOrUndefInt"),
                    Expression.Constant(runtime),
                    idx.Expression,
                    Expression.Constant(create)),
                Utils.RestrictToRuntimeType(target)
                    .Merge(idx.Restrictions));
        }

        private DynamicMetaObject BindFallback(DynamicMetaObject target, DynamicMetaObject index)
        {
            var idx = BinderUtils.ConvertInteger(runtime, index);

            return new DynamicMetaObject(
                Expression.Call(
                    typeof(Builtins).GetMethod("GetListItemOrUndefInt"),
                    Utils.CastRuntime(target),
                    idx.Expression,
                    Expression.Constant(create)),
                Utils.RestrictToRuntimeType(target)
                    .Merge(idx.Restrictions));
        }

        private Runtime runtime;
        private bool create;
    }
}
