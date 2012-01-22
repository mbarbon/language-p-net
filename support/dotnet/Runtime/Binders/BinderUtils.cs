using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;

namespace org.mbarbon.p.runtime
{
    class BinderUtils
    {
        public static DynamicMetaObject ConvertInteger(Runtime runtime, DynamicMetaObject target)
        {
            if (Utils.IsAny(target))
                return new DynamicMetaObject(
                    Expression.Call(
                        Utils.CastRuntime(target),
                        target.RuntimeType.GetMethod("AsInteger"),
                        Expression.Constant(runtime)),
                    Utils.RestrictToRuntimeType(target));
            if (Utils.IsInteger(target))
                return new DynamicMetaObject(
                    Expression.Convert(target.Expression, typeof(int)),
                    Utils.RestrictToRuntimeType(target));

            throw new System.Exception("Unhandled integer conversion");
        }

        public static DynamicMetaObject ConvertBoolean(Runtime runtime, DynamicMetaObject target)
        {
            if (Utils.IsAny(target))
                return new DynamicMetaObject(
                    Expression.Call(
                        Utils.CastRuntime(target),
                        target.RuntimeType.GetMethod("AsBoolean"),
                        Expression.Constant(runtime)),
                    Utils.RestrictToRuntimeType(target));
            if (Utils.IsInteger(target))
                return new DynamicMetaObject(
                    Expression.NotEqual(
                        target.Expression,
                        Expression.Constant(0)),
                    Utils.RestrictToRuntimeType(target));
            if (Utils.IsFloat(target))
                return new DynamicMetaObject(
                    Expression.NotEqual(
                        target.Expression,
                        Expression.Constant(0.0)),
                    Utils.RestrictToRuntimeType(target));
            if (Utils.IsString(target))
                // FIXME fix "0" -> false
                return new DynamicMetaObject(
                    Expression.NotEqual(
                        Expression.Property(
                            target.Expression,
                            typeof(string).GetProperty("Length")),
                        Expression.Constant(0)),
                    Utils.RestrictToRuntimeType(target));

            throw new System.Exception("Unhandled integer conversion");
        }

        public static DynamicMetaObject ConvertString(Runtime runtime, DynamicMetaObject arg)
        {
            if (Utils.IsString(arg))
                return new DynamicMetaObject(
                    Expression.Convert(arg.Expression, typeof(string)),
                    Utils.RestrictToRuntimeType(arg));
            else if (Utils.IsAny(arg))
                return new DynamicMetaObject(
                    Expression.Call(
                        Utils.CastAny(arg),
                        typeof(IP5Any).GetMethod("AsString"),
                        Expression.Constant(runtime)),
                    Utils.RestrictToRuntimeType(arg));
            else
                return new DynamicMetaObject(
                    Expression.Call(
                        arg.Expression,
                        typeof(object).GetMethod("ToString")),
                    BindingRestrictions.Empty);
        }
    }
}
