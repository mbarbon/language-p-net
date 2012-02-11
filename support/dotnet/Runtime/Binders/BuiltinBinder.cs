using org.mbarbon.p.values;

using System.Dynamic;
using Microsoft.Scripting.Ast;
using Type = System.Type;
using MethodInfo = System.Reflection.MethodInfo;
using System.Collections.Generic;
using IEnumerable = System.Collections.IEnumerable;
using IEnumerator = System.Collections.IEnumerator;

namespace org.mbarbon.p.runtime
{
    public class P5BuiltinBinder : DynamicMetaObjectBinder
    {
        public P5BuiltinBinder(Runtime _runtime, string _prefix, int _count)
        {
            runtime = _runtime;
            prefix = _prefix;
            count = _count;
        }

        private IEnumerator<string> EnumerateTypeNames(Type type)
        {
            // System.Console.WriteLine(type.Name);

            // TODO use IList where possible
            if (type.Name == "List`1")
                yield return "List";
            else
                yield return type.Name;

            if (typeof(IP5Any).IsAssignableFrom(type))
                yield return "IP5Any";
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                yield return "Enumerator";

            yield return "Object";
        }

        private IEnumerable<string> EnumerateSuffixes(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (count == 0)
            {
                yield return "";
                yield break;
            }

            var parts = new IEnumerator<string>[count];
            int level = 1;

            parts[0] = EnumerateTypeNames(target.RuntimeType);

            for (;;)
            {
                for (int i = level - 1; i >= 0; --i)
                {
                    if (parts[i] == null || parts[i].MoveNext())
                        break;

                    level -= 1;
                }

                if (level == 0)
                    break;

                while (level < count)
                {
                    parts[level] = EnumerateTypeNames(args[level - 1].RuntimeType);
                    parts[level].MoveNext();
                    level += 1;
                }

                var builder = new System.Text.StringBuilder();

                foreach (var part in parts)
                    builder.Append(part.Current);

                yield return builder.ToString();
            }

            yield return "";
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            // map types:
            // *P5*        -> *P5*, fallback to various interfaces
            // IEnumerable -> Enumerable
            // IList       -> IList, Enumerable
            // List<T>     -> List, Enumerable
            // ...
            // in any case, fallback to Object
            //
            // if IP5Any subclass search for matching method
            // otherwise search static method in Builtins
            //
            // call the builtin casting parameters as appropriate

            bool is_any = Utils.IsAny(target);

            foreach (var suffix in EnumerateSuffixes(target, args))
            {
                // System.Console.WriteLine(prefix + " '" + suffix + "'");

                var bmethod = typeof(Builtins).GetMethod(prefix + suffix);
                var omethod = is_any ? target.RuntimeType.GetMethod(prefix + suffix) : null;

                if (omethod != null)
                    return BindMethod(omethod, target, args);
                if (bmethod != null)
                    return BindFunction(bmethod, target, args);
            }

            throw new System.Exception("Implement me " + prefix);
        }

        private DynamicMetaObject BindFunction(MethodInfo method, DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var parms = method.GetParameters();
            var exps = new Expression[parms.Length];
            var restrictions = BindingRestrictions.Empty;
            int pindex = 0, aindex = 0;

            // special case: if the first argument is a Runtime, pass
            // it implicity
            if (parms.Length > 0 && parms[0].ParameterType == typeof(Runtime))
            {
                exps[aindex++] = Expression.Constant(runtime);
                pindex += 1;
            }

            exps[aindex++] = ConvertArgument(target, parms[pindex++].ParameterType, ref restrictions);

            for (int i = pindex; i < parms.Length; ++i)
                exps[aindex++] = ConvertArgument(args[i - pindex], parms[i].ParameterType, ref restrictions);

            return new DynamicMetaObject(
                Expression.Call(method, exps),
                restrictions);
        }

        private DynamicMetaObject BindMethod(MethodInfo method, DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var parms = method.GetParameters();
            var exps = new Expression[parms.Length];
            var restrictions = BindingRestrictions.Empty;
            int pindex = 0, aindex = 0;

            // special case: if the first argument is a Runtime, pass
            // it implicity
            if (parms.Length > 0 && parms[0].ParameterType == typeof(Runtime))
            {
                exps[aindex++] = Expression.Constant(runtime);
                pindex += 1;
            }

            for (int i = pindex; i < parms.Length; ++i)
                exps[aindex++] = ConvertArgument(args[i - pindex], parms[i].ParameterType, ref restrictions);

            return new DynamicMetaObject(
                Expression.Call(Utils.CastRuntime(target), method, exps),
                restrictions);
        }

        private Expression ConvertArgument(DynamicMetaObject arg, Type type, ref BindingRestrictions restrictions)
        {
            if (arg.RuntimeType == type || type.IsAssignableFrom(arg.RuntimeType))
            {
                restrictions = restrictions.Merge(Utils.RestrictToRuntimeType(arg));

                return Expression.Convert(arg.Expression, type);
            }

            if (type == typeof(IEnumerator) && typeof(IEnumerable).IsAssignableFrom(arg.RuntimeType))
            {
                restrictions = restrictions.Merge(Utils.RestrictToRuntimeType(arg));

                return Expression.Call(
                    Expression.Convert(
                        arg.Expression, typeof(IEnumerable)),
                    typeof(IEnumerable).GetMethod("GetEnumerator"));
            }

            if (type == typeof(string))
            {
                var dmo = BinderUtils.ConvertString(runtime, arg);

                restrictions = restrictions.Merge(dmo.Restrictions);

                return dmo.Expression;
            }
            else
                throw new System.Exception("Implement me " + arg.RuntimeType + " " + type.Name);
        }

        private Runtime runtime;
        private string prefix;
        private int count;
    }
}
