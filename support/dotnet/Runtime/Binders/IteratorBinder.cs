using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;
using System.Collections.Generic;
using IEnumerable = System.Collections.IEnumerable;

namespace org.mbarbon.p.runtime
{
    public class P5IteratorBinder : DynamicMetaObjectBinder
    {
        public P5IteratorBinder(Runtime _runtime)
        {
            runtime = _runtime;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (Utils.IsArray(target))
                return BindIP5Enumerable(target);
            if (target.RuntimeType == typeof(List<object>))
                return BindList(target);

            throw new System.Exception("Unable to bind " + target.RuntimeType);
        }

        private DynamicMetaObject BindIP5Enumerable(DynamicMetaObject target)
        {
            return new DynamicMetaObject(
                Expression.Call(
                    Utils.CastRuntime(target),
                    typeof(IP5Enumerable).GetMethod("GetEnumerator"),
                    Expression.Constant(runtime)),
                Utils.RestrictToRuntimeType(target));
        }

        private DynamicMetaObject BindList(DynamicMetaObject target)
        {
            return new DynamicMetaObject(
                Expression.Call(
                    Utils.CastRuntime(target),
                    typeof(IEnumerable).GetMethod("GetEnumerator")),
                Utils.RestrictToRuntimeType(target));
        }

        private Runtime runtime;
    }
}
