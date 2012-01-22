using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5HashItemBinder : DynamicMetaObjectBinder
    {
        public P5HashItemBinder(Runtime _runtime, bool _create)
        {
            runtime = _runtime;
            create = _create;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (Utils.IsHash(target))
                return BindIP5Hash(target, args[0]);

            throw new System.Exception("Implement hash assignment for hash-like objects");
        }

        private DynamicMetaObject BindIP5Hash(DynamicMetaObject target, DynamicMetaObject index)
        {
            var idx = BinderUtils.ConvertString(runtime, index);

            return new DynamicMetaObject(
                Expression.Call(
                    Utils.CastRuntime(target),
                    typeof(IP5Hash).GetMethod("GetItemOrUndefString"),
                    Expression.Constant(runtime),
                    idx.Expression,
                    Expression.Constant(create)),
                Utils.RestrictToRuntimeType(target)
                    .Merge(idx.Restrictions));
        }

        private Runtime runtime;
        private bool create;
    }
}
