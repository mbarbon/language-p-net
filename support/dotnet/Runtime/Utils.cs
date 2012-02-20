using org.mbarbon.p.values;

using IList = System.Collections.IList;
using System; // Func
using System.Dynamic;
using Microsoft.Scripting.Ast;
using System.Runtime.CompilerServices;

// TODO only for .Net 3.5
public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, out TResult>(
	T1 arg1,
	T2 arg2,
	T3 arg3,
	T4 arg4,
	T5 arg5);

namespace org.mbarbon.p.runtime
{
    class Utils
    {
        public static bool IsAny(DynamicMetaObject o)
        {
            return typeof(IP5Any).IsAssignableFrom(o.RuntimeType);
        }

        public static bool IsNull(DynamicMetaObject o)
        {
            return o.RuntimeType == null;
        }

        public static bool IsValue(DynamicMetaObject o)
        {
            return typeof(IP5Value).IsAssignableFrom(o.RuntimeType);
        }

        public static bool IsScalar(DynamicMetaObject o)
        {
            return typeof(P5Scalar).IsAssignableFrom(o.RuntimeType);
        }

        public static bool IsArray(DynamicMetaObject o)
        {
            return typeof(IP5Array).IsAssignableFrom(o.RuntimeType);
        }

        public static bool IsHash(DynamicMetaObject o)
        {
            return typeof(IP5Hash).IsAssignableFrom(o.RuntimeType);
        }

        public static bool IsList(DynamicMetaObject o)
        {
            return typeof(IList).IsAssignableFrom(o.RuntimeType);
        }

        public static bool IsInteger(DynamicMetaObject o)
        {
            return o.RuntimeType == typeof(int);
        }

        public static bool IsFloat(DynamicMetaObject o)
        {
            return o.RuntimeType == typeof(double);
        }

        public static bool IsBoolean(DynamicMetaObject o)
        {
            return o.RuntimeType == typeof(bool);
        }

        public static bool IsString(DynamicMetaObject o)
        {
            return o.RuntimeType == typeof(string);
        }

        public static Expression CastObject(DynamicMetaObject o)
        {
            return Expression.Convert(o.Expression, typeof(object));
        }

        public static Expression CastAny(DynamicMetaObject o)
        {
            return Expression.Convert(o.Expression, typeof(IP5Any));
        }

        public static Expression CastValue(DynamicMetaObject o)
        {
            return Expression.Convert(o.Expression, typeof(IP5Value));
        }

        public static Expression CastScalar(DynamicMetaObject o)
        {
            return Expression.Convert(o.Expression, typeof(P5Scalar));
        }

        public static Expression CastRuntime(DynamicMetaObject o)
        {
            return Expression.Convert(o.Expression, o.RuntimeType);
        }

        public static Expression CastInteger(DynamicMetaObject o)
        {
            return Expression.Convert(o.Expression, typeof(int));
        }

        public static Expression CastFloat(DynamicMetaObject o)
        {
            return Expression.Convert(o.Expression, typeof(double));
        }

        public static BindingRestrictions RestrictToRuntimeType(DynamicMetaObject a, DynamicMetaObject b)
        {
            return RestrictToRuntimeType(a)
                .Merge(RestrictToRuntimeType(b));
        }

        public static BindingRestrictions RestrictToRuntimeType(DynamicMetaObject a)
        {
            if (a.RuntimeType == null)
                return BindingRestrictions.GetInstanceRestriction(a.Expression, null);

            return BindingRestrictions.GetTypeRestriction(a.Expression, a.RuntimeType);
        }

        public static BindingRestrictions RestrictToScalar(DynamicMetaObject a, DynamicMetaObject b)
        {
            return BindingRestrictions.GetTypeRestriction(a.Expression, typeof(P5Scalar))
                .Merge(BindingRestrictions.GetTypeRestriction(b.Expression, typeof(P5Scalar)));
        }

        public static BindingRestrictions RestrictToScalar(DynamicMetaObject a)
        {
            return BindingRestrictions.GetTypeRestriction(a.Expression, typeof(P5Scalar));
        }

        public static BindingRestrictions RestrictToAny(DynamicMetaObject a, DynamicMetaObject b)
        {
            // no way to restrict to an interface: restrict to the type
            return RestrictToRuntimeType(a, b);
        }

        public static BindingRestrictions RestrictToAny(DynamicMetaObject a)
        {
            // no way to restrict to an interface: restrict to the type
            return RestrictToRuntimeType(a);
        }

        public static BindingRestrictions RestrictToInteger(DynamicMetaObject a)
        {
            return BindingRestrictions.GetTypeRestriction(a.Expression, typeof(int));
        }

        public static BindingRestrictions RestrictToFloat(DynamicMetaObject a)
        {
            return BindingRestrictions.GetTypeRestriction(a.Expression, typeof(double));
        }

        public static BindingRestrictions RestrictToBoolean(DynamicMetaObject a)
        {
            return BindingRestrictions.GetTypeRestriction(a.Expression, typeof(bool));
        }

        public static BindingRestrictions RestrictToString(DynamicMetaObject a)
        {
            return BindingRestrictions.GetTypeRestriction(a.Expression, typeof(string));
        }

        public static Expression GenerateCall(Expression[] expressions, DynamicMetaObjectBinder binder)
        {
            System.Type delegateType, siteType;
            CallSite callSite;

            // TODO could use reflection
            switch (expressions.Length - 1)
            {
            case 1:
                delegateType = typeof(Func<CallSite, object, object>);
                siteType = typeof(CallSite<Func<CallSite, object, object>>);
                callSite = CallSite<Func<CallSite, object, object>>.Create(binder);
                break;
            case 2:
                delegateType = typeof(Func<CallSite, object, object, object>);
                siteType = typeof(CallSite<Func<CallSite, object, object, object>>);
                callSite = CallSite<Func<CallSite, object, object, object>>.Create(binder);
                break;
            case 3:
                delegateType = typeof(Func<CallSite, object, object, object, object>);
                siteType = typeof(CallSite<Func<CallSite, object, object, object, object>>);
                callSite = CallSite<Func<CallSite, object, object, object, object>>.Create(binder);
                break;
            case 4:
                delegateType = typeof(Func<CallSite, object, object, object, object, object>);
                siteType = typeof(CallSite<Func<CallSite, object, object, object, object, object>>);
                callSite = CallSite<Func<CallSite, object, object, object, object, object>>.Create(binder);
                break;
            default:
                throw new System.Exception("Unhandled argument count " + expressions.Length);
            }

            expressions[0] = Expression.Constant(callSite);

            return Expression.Call(
                Expression.Field(
                    Expression.Constant(callSite),
                    siteType.GetField("Target")),
                delegateType.GetMethod("Invoke"),
                expressions);
        }
    }
}
