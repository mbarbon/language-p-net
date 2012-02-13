using org.mbarbon.p.values;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        public static object CallMethod(Runtime runtime, List<object> args, Opcode.ContextValues context, string method)
        {
            var invocant = args[0] as P5Scalar;
            var pack = args[0] as string;

            if (invocant != null)
                return invocant.CallMethod(runtime, context, method, args);
            else
                throw new System.Exception("Implement for string");
        }

        public static object CallMethodIndirect(Runtime runtime, List<object> args, Opcode.ContextValues context, P5Scalar method)
        {
            var pmethod = method.IsReference(runtime) ? method.Dereference(runtime) as P5Code : null;

            if (pmethod != null)
                return pmethod.Call(runtime, context, args);

            return CallMethod(runtime, args, context, method.AsString(runtime));
        }

        public static bool IsDerivedFrom(Runtime runtime, object value, string pack)
        {
            var scalar = value as P5Scalar;
            P5SymbolTable stash = null;

            if (scalar != null)
                scalar.BlessedReferenceStash(runtime);

            if (stash == null)
                stash = runtime.SymbolTable.GetPackage(runtime, Builtins.ConvertToString(runtime, value), false);

            P5SymbolTable parent = runtime.SymbolTable.GetPackage(runtime, pack, false);

            if (parent == null || stash == null)
                return false;

            return stash.IsDerivedFrom(runtime, parent);
        }
    }
}
