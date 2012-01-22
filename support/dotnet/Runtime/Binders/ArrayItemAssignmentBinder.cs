using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    public class P5ArrayItemAssignmentBinder : DynamicMetaObjectBinder
    {
        public P5ArrayItemAssignmentBinder(Runtime _runtime)
        {
            runtime = _runtime;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (Utils.IsArray(target))
                return BindIP5Array(target, args[0], args[1]);

            return BindFallback(target, args[0], args[1]);
        }

        private DynamicMetaObject BindIP5Array(DynamicMetaObject target, DynamicMetaObject index, DynamicMetaObject value)
        {
            var idx = BinderUtils.ConvertInteger(runtime, index);

            // TODO handle tied arrays
            return new DynamicMetaObject(
                Expression.Call(
                    Expression.Convert(
                        Expression.Call(
                            Utils.CastRuntime(target),
                            typeof(IP5Array).GetMethod("GetItemOrUndefInt"),
                            Expression.Constant(runtime),
                            idx.Expression,
                            Expression.Constant(true)),
                        typeof(P5Scalar)),
                    typeof(P5Scalar).GetMethod("AssignObject"),
                    Expression.Constant(runtime),
                    Utils.CastObject(value)),
                Utils.RestrictToRuntimeType(value, target)
                    .Merge(idx.Restrictions));
        }

        private DynamicMetaObject BindFallback(DynamicMetaObject target, DynamicMetaObject index, DynamicMetaObject value)
        {
            var idx = BinderUtils.ConvertInteger(runtime, index);
            var item = Expression.MakeIndex(
                Utils.CastRuntime(target),
                target.RuntimeType.GetProperty("Item"),
                new Expression[] { idx.Expression });

            return new DynamicMetaObject(
                Expression.Assign(
                    item,
                    Expression.Call(
                        typeof(Builtins).GetMethod("AssignArrayItem"),
                        Expression.Constant(runtime),
                        item,
                        value.Expression)),
                Utils.RestrictToRuntimeType(value, target)
                    .Merge(idx.Restrictions));
        }

        private Runtime runtime;
    }
}
