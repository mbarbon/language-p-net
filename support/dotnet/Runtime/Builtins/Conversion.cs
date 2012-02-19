using org.mbarbon.p.values;
using IdDispenser = Microsoft.Scripting.Runtime.IdDispenser;
using IList = System.Collections.IList;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        public static string ConvertToString(Runtime runtime, object value)
        {
            var iany = value as IP5Any;

            if (iany != null)
                return iany.AsScalar(runtime).AsString(runtime);
            if (value is string)
                return (string)value;
            if (value is int)
                return value.ToString();
            if (value is double)
                return System.String.Format("{0:0.#########}", (double)value);
            if (value is bool)
                return (bool)value ? "1" : "0";
            if (value == null)
                // TODO warn
                return "";

            throw new System.Exception("Unhandled type in string coercion");
        }

        public static string ConvertToKeyString(Runtime runtime, object value)
        {
            var iany = value as IP5Any;

            if (iany != null)
                return iany.AsScalar(runtime).KeyString(runtime);
            if (value is string)
                return (string)value;
            if (value is int)
                return value.ToString();
            if (value is double)
                return System.String.Format("{0:0.#########}", (double)value);
            if (value is bool)
                return (bool)value ? "1" : "0";

            return IdDispenser.GetId(value).ToString();
        }

        public static int ConvertToInteger(Runtime runtime, object value)
        {
            var iany = value as IP5Any;

            if (iany != null)
                return iany.AsScalar(runtime).AsInteger(runtime);
            if (value is int)
                return (int) value;
            if (value is double)
                return (int) (double) value;
            if (value is bool)
                return (bool)value ? 1 : 0;

            throw new System.Exception("Unhandled type in integer coercion");
        }

        public static double ConvertToFloat(Runtime runtime, object value)
        {
            var iany = value as IP5Any;

            if (iany != null)
                return iany.AsScalar(runtime).AsFloat(runtime);
            if (value is int)
                return (double) (int) value;
            if (value is double)
                return (double) value;
            if (value is bool)
                return (bool) value ? 1.0 : 0.0;

            throw new System.Exception("Unhandled type in float coercion");
        }

        public static object ConvertToScalarValue(Runtime runtime, object value)
        {
            var iany = value as IP5Any;
            var ilist = value as IList;

            if (iany != null)
                return iany.AsScalar(runtime);
            if (ilist != null)
                return ilist.Count;

            return value;
        }
    }
}