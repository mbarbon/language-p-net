using org.mbarbon.p.values;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        public static void TracePosition(string file, int line)
        {
            System.Console.WriteLine(string.Format("{0:S}:{1:D}", file, line));
        }

        public static P5Scalar UpgradeScalar(Runtime runtime, object val)
        {
            P5Scalar value = val as P5Scalar;

            if (value != null)
                return value;
            if (val == null)
                return new P5Scalar(runtime);
            if (val is int)
                return new P5Scalar(runtime, (int)val);
            if (val is string)
                return new P5Scalar(runtime, (string)val);
            if (val is double)
                return new P5Scalar(runtime, (double)val);

            return new P5Scalar(new P5NetWrapper(val));
        }

        public static IP5Referrable UpgradeReferrable(Runtime runtime, object val)
        {
            IP5Referrable value = val as IP5Referrable;

            if (value != null)
                return value;
            // TODO should probably use a constant scalar
            if (val == null)
                return new P5Scalar(runtime);
            if (val is int)
                return new P5Scalar(runtime, (int)val);
            if (val is string)
                return new P5Scalar(runtime, (string)val);
            if (val is double)
                return new P5Scalar(runtime, (double)val);

            return new P5Scalar(new P5NetWrapper(val));
        }

        public static object Return(Runtime runtime, Opcode.ContextValues cxt,
                                    object val)
        {
            IP5Any value = val as IP5Any;

            // TODO handle scalar/list context for List/IList...
            if (value == null)
                return val;

            if (cxt == Opcode.ContextValues.SCALAR)
                return value.AsScalar(runtime);
            if (cxt == Opcode.ContextValues.LIST)
                return value as P5Array ?? new P5List(runtime, value);

            return P5List.EmptyList;
        }

        public static P5Scalar WantArray(Runtime runtime, Opcode.ContextValues cxt)
        {
            if (cxt == Opcode.ContextValues.VOID)
                return new P5Scalar(runtime);

            return new P5Scalar(runtime, cxt == Opcode.ContextValues.LIST);
        }

        public static int ParseOctal(string s, int start)
        {
            int end;

            for (end = start; end < s.Length; ++end)
                if (!(s[end] >= '0' && s[end] <= '7'))
                    break;

            if (start == end)
                return 0;

            return System.Convert.ToInt32(s.Substring(start, end - start), 8);
        }

        public static int ParseHexadecimal(string s, int start)
        {
            int end;

            for (end = start; end < s.Length; ++end)
                if (!(   (s[end] >= '0' && s[end] <= '9')
                      || (s[end] >= 'a' && s[end] <= 'f')
                      || (s[end] >= 'A' && s[end] <= 'F')))
                    break;

            if (start == end)
                return 0;

            return System.Convert.ToInt32(s.Substring(start, end - start), 16);
        }

        public static int ParseBinary(string s, int start)
        {
            int end;

            for (end = start; end < s.Length; ++end)
                if (s[end] != '0' && s[end] != '1')
                    break;

            if (start == end)
                return 0;

            return System.Convert.ToInt32(s.Substring(start, end - start), 2);
        }

        public static int ParseInteger(string s)
        {
            if (s.Length == 0)
                return 0;
            if (s[0] != '-' && !char.IsDigit(s[0]) && !char.IsWhiteSpace(s[0]))
                return 0;

            // TODO this does not work for " 123", "-1_234", "12abc"
            return int.Parse(s);
        }

        public static double ParseFloat(string s)
        {
            if (s.Length == 0)
                return 0;
            if (s[0] != '-' && !char.IsDigit(s[0]) && !char.IsWhiteSpace(s[0]) && s[0] != '.')
                return 0;

            // TODO this does not work for " 123", "-1_234", "12abc"
            return double.Parse(s);
        }

        public static int ParseBaseInteger(string s, int start, int num_base)
        {
            int rem = s.Length - start;

            if (rem > 2)
            {
                if (s[start] == '0' && s[start + 1] == 'x')
                {
                    num_base = 16;
                    start += 2;
                }
                else if (s[start] == '0' && s[start + 1] == 'b')
                {
                    num_base = 2;
                    start += 2;
                }
            }

            if (num_base == 2)
                return ParseBinary(s, start);
            else if (num_base == 8)
                return ParseOctal(s, start);
            else if (num_base == 16)
                return ParseHexadecimal(s, start);

            return 0; // can't happen
        }

        public static P5Scalar Negate(Runtime runtime, P5Scalar value)
        {
            if (value.IsString(runtime))
            {
                string str = value.AsString(runtime);
                bool word = true;

                foreach (var c in str)
                {
                    // FIXME WTF? why the check
                    if (!(c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                    {
                        word = false;
                        break;
                    }
                }

                if (word)
                    return new P5Scalar(runtime, "-" + str);
            }

            if (value.IsFloat(runtime))
                return new P5Scalar(runtime, -value.AsFloat(runtime));

            return new P5Scalar(runtime, -value.AsInteger(runtime));
        }

        public static P5List SplitSpaces(Runtime runtime, IP5Any value)
        {
            var str = value.AsString(runtime);
            var res = new P5List(runtime);
            int start = 0, curr = 0;

            for ( ; curr < str.Length; )
            {
                for ( ; curr < str.Length && char.IsWhiteSpace(str[curr]); ++curr)
                    ;
                start = curr;
                if (start == str.Length)
                    break;
                for ( ; curr < str.Length && !char.IsWhiteSpace(str[curr]); ++curr)
                    ;

                res.Push(runtime, new P5Scalar(runtime, str.Substring(start, curr - start)));
            }

            return res;
        }

        public static IP5Any Warn(Runtime runtime, P5Array args)
        {
            // TODO handle empty argument list when $@ is set and when it is not

            var message = new System.Text.StringBuilder();

            for (var it = args.GetEnumerator(runtime); it.MoveNext(); )
                message.Append(Builtins.ConvertToString(runtime, it.Current));

            if (message.Length > 0 && message[message.Length - 1] != '\n')
                message.Append(string.Format(" at {0:S} line {1:D}.\n",
                                             runtime.File, runtime.Line));

            var stderr = runtime.SymbolTable.GetGlob(runtime, "STDERR", true);

            stderr.Handle.Write(runtime, message.ToString());

            return new P5Scalar(runtime, 1);
        }

        public static P5Exception Die(Runtime runtime, List<object> args)
        {
            int argc = args.Count;

            if (argc == 1)
            {
                var s = args[0] as P5Scalar;

                if (s != null && s.IsReference(runtime))
                    return new P5Exception(runtime, s);
            }

            string message;
            if (argc == 0)
            {
                var exc = runtime.SymbolTable.GetStashScalar(runtime, "@", true);

                if (exc.IsDefined(runtime))
                    message = exc.AsString(runtime) + "\t...propagated";
                else
                    message = "Died";
            }
            else
            {
                var t = new System.Text.StringBuilder();
                foreach (var e in args)
                    t.Append(Builtins.ConvertToString(runtime, e));
                message = t.ToString();
            }

            return new P5Exception(runtime, message);
        }

        public static IP5Any LocalizeArrayElement(Runtime runtime, IP5Array array, IP5Any index, ref SavedValue state)
        {
            int int_index = array.GetItemIndex(runtime, index.AsInteger(runtime), true);
            var saved = array.LocalizeElement(runtime, int_index);

            state.container = array;
            state.int_key = int_index;
            state.value = saved;

            return array.GetItem(runtime, int_index);
        }

        public static void RestoreArrayElement(Runtime runtime, ref SavedValue state)
        {
            if (state.container == null)
                return;

            (state.container as IP5Array).RestoreElement(runtime, state.int_key, state.value);
            state.container = null;
            state.str_key = null;
            state.value = null;
        }

        public static IP5Any LocalizeHashElement(Runtime runtime, P5Hash hash, IP5Any index, ref SavedValue state)
        {
            string str_index = index.AsString(runtime);
            var saved = hash.LocalizeElement(runtime, str_index);
            var new_value = new P5Scalar(runtime);

            state.container = hash;
            state.str_key = str_index;
            state.value = saved;

            hash.SetItem(runtime, str_index, new_value);

            return new_value;
        }

        public static void RestoreHashElement(Runtime runtime, ref SavedValue state)
        {
            if (state.container == null)
                return;

            (state.container as P5Hash).RestoreElement(runtime, state.str_key, state.value);
            state.container = null;
            state.str_key = null;
            state.value = null;
        }

        public static P5Range MakeRange(Runtime runtime, int start, int end)
        {
            // TODO handle string range
            return new P5Range(runtime, start, end);
        }

        public static IP5Regex CompileRegex(Runtime runtime, P5Scalar value, int flags)
        {
            if (value.IsReference(runtime))
            {
                var rx = value.DereferenceRegex(runtime);

                if (rx != null)
                    return rx;
            }

            if (runtime.NativeRegex)
                return new NetRegex(value.AsString(runtime));
            else
                throw new System.Exception("P5: Needs compiler to recompile string expression");
        }

        public static int Transliterate(Runtime runtime, P5Scalar scalar,
                                        string match, string replacement,
                                        int flags)
        {
            bool complement = (flags & Opcode.FLAG_RX_COMPLEMENT) != 0;
            bool delete = (flags & Opcode.FLAG_RX_DELETE) != 0;
            bool squeeze = (flags & Opcode.FLAG_RX_SQUEEZE) != 0;
            var s = scalar.AsString(runtime);
            int count = 0, last_r = -1;
            var new_str = new System.Text.StringBuilder();

            for (int i = 0; i < s.Length; ++i)
            {
                int idx = match.IndexOf(s[i]);
                int replace = s[i];

                if (idx == -1 && complement)
                {
                    if (delete)
                        replace = -1;
                    else if (replacement.Length > 0)
                        replace = replacement[replacement.Length - 1];

                    if (last_r == replace && squeeze)
                        replace = -1;
                    else
                        last_r = replace;

                    count += 1;
                }
                else if (idx != -1 && !complement)
                {
                    if (idx >= replacement.Length && delete)
                        replace = -1;
                    else if (idx >= replacement.Length)
                        replace = replacement[replacement.Length - 1];
                    else if (idx < replacement.Length)
                        replace = replacement[idx];

                    if (last_r == replace && squeeze)
                        replace = -1;
                    else
                        last_r = replace;

                    count += 1;
                }
                else
                    last_r = -1;

                if (replace != -1)
                    new_str.Append((char)replace);
            }

            scalar.SetString(runtime, new_str.ToString());

            return count;
        }

        public static IP5Any HashEach(Runtime runtime, Opcode.ContextValues cxt, P5Hash hash)
        {
            P5Scalar key, value;

            if (hash.NextKey(runtime, out key, out value))
            {
                if (cxt == Opcode.ContextValues.SCALAR)
                    return key;
                else
                    return new P5List(runtime, key, value);
            }
            else
            {
                if (cxt == Opcode.ContextValues.SCALAR)
                    return new P5Scalar(runtime);
                else
                    return new P5List(runtime);
            }
        }

        public static P5Scalar Oct(Runtime runtime, P5Scalar value)
        {
            var str = value.AsString(runtime);
            int start;

            for (start = 0; start < str.Length; ++start)
                if (!char.IsWhiteSpace(str[start]))
                    break;

            // TODO warn about invalid octal digit
            return new P5Scalar(runtime, ParseBaseInteger(str, start, 8));
        }

        public static P5Scalar Hex(Runtime runtime, P5Scalar value)
        {
            var str = value.AsString(runtime);

            // TODO warn about invalid hexadecimal digits
            return new P5Scalar(runtime, ParseBaseInteger(str, 0, 16));
        }

        public static P5Scalar Index(Runtime runtime, IP5Any value,
                                     IP5Any substr, int start)
        {
            var str = value.AsString(runtime);
            var sub = substr.AsString(runtime);

            if (start < 0)
                start = 0;
            if (start > str.Length)
                start = str.Length;

            return new P5Scalar(runtime, str.IndexOf(sub, start));
        }

        public static void AddOverload(Runtime runtime, string pack_name,
                                       P5Array args)
        {
            var overloads = new Overloads();
            var pack = runtime.SymbolTable.GetPackage(runtime, pack_name, true);

            for (int i = 0; i < args.GetCount(runtime); i += 2)
            {
                string key = args.GetItem(runtime, i).AsString(runtime);
                var value = args.GetItem(runtime, i + 1);

                overloads.AddOperation(runtime, key, value);
            }

            pack.SetOverloads(overloads);
        }

        public static bool IsOverloaded(Runtime runtime, object value,
                                        out Overloads overloads)
        {
            overloads = null;
            var scalar = value as P5Scalar;

            return scalar == null ? false : IsOverloaded(runtime, scalar,
                                                         out overloads);
        }

        public static bool IsOverloaded(Runtime runtime, P5Scalar scalar,
                                        out Overloads overloads)
        {
            overloads = null;

            if (!scalar.IsReference(runtime))
                return false;

            var stash = scalar.Dereference(runtime).Blessed(runtime);
            if (stash == null)
                return false;

            overloads = stash.Overloads;

            return stash.HasOverloading;
        }

        public static P5Scalar CallOverload(Runtime runtime, OverloadOperation op,
                                            P5Scalar left, object right)
        {
            Overloads oleft, oright;

            if (   !IsOverloaded(runtime, left, out oleft)
                && !IsOverloaded(runtime, right, out oright))
                return null;

            Overloads overload = oleft ?? oright;

            return overload.CallOperation(runtime, op, left, right,
                                          overload == oright);
        }

        public static P5Scalar CallOverloadInverted(Runtime runtime, OverloadOperation op,
                                                    object left, P5Scalar right)
        {
            Overloads oright;

            if (!IsOverloaded(runtime, right, out oright))
                return null;

            return oright.CallOperation(runtime, op, left, right, true);
        }
    }
}
