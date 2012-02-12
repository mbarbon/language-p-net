using org.mbarbon.p.values;

namespace org.mbarbon.p.runtime
{
    public partial class Builtins
    {
        public static string BitNotString(Runtime runtime, string value)
        {
            var t = new System.Text.StringBuilder(value);;

            for (int i = 0; i < value.Length; ++i)
                t[i] = (char)(~t[i] & 0xff); // only ASCII for now

            return t.ToString();
        }

        public static object BitNot(Runtime runtime, P5Scalar value)
        {
            if (value.IsString(runtime))
            {
                return BitNotString(runtime, value.AsString(runtime));
            }
            else
            {
                // TODO take into account signed/unsigned?
                return ~value.AsInteger(runtime);
            }
        }

        public static object BitOrScalarScalarAssign(Runtime runtime,
                                                     P5Scalar a, P5Scalar b)
        {
            a.AssignObject(runtime, BitOrScalarScalar(runtime, a, b));

            return a;
        }

        public static object BitOrScalarScalar(Runtime runtime,
                                               P5Scalar a, P5Scalar b)
        {
            if (a.IsString(runtime) && b.IsString(runtime))
            {
                string sa = a.AsString(runtime), sb = b.AsString(runtime);
                System.Text.StringBuilder t;

                if (sa.Length > sb.Length)
                {
                    t = new System.Text.StringBuilder(sa);

                    for (int i = 0; i < sb.Length; ++i)
                        t[i] |= sb[i];
                }
                else
                {
                    t = new System.Text.StringBuilder(sb);

                    for (int i = 0; i < sa.Length; ++i)
                        t[i] |= sa[i];
                }

                return t.ToString();
            }
            else
            {
                // TODO take into account signed/unsigned?
                return a.AsInteger(runtime) | b.AsInteger(runtime);
            }
        }

        public static object BitXorScalarScalarAssign(Runtime runtime,
                                                      P5Scalar a, P5Scalar b)
        {
            a.AssignObject(runtime, BitXorScalarScalar(runtime, a, b));

            return a;
        }

        public static object BitXorScalarScalar(Runtime runtime,
                                                P5Scalar a, P5Scalar b)
        {
            if (a.IsString(runtime) && b.IsString(runtime))
            {
                string sa = a.AsString(runtime), sb = b.AsString(runtime);
                System.Text.StringBuilder t;

                if (sa.Length > sb.Length)
                {
                    t = new System.Text.StringBuilder(sa);

                    for (int i = 0; i < sb.Length; ++i)
                        t[i] ^= sb[i];
                }
                else
                {
                    t = new System.Text.StringBuilder(sb);

                    for (int i = 0; i < sa.Length; ++i)
                        t[i] ^= sa[i];
                }

                return t.ToString();
            }
            else
            {
                // TODO take into account signed/unsigned?
                return a.AsInteger(runtime) ^ b.AsInteger(runtime);
            }
        }

        public static object BitAndScalarScalarAssign(Runtime runtime,
                                                      P5Scalar a, P5Scalar b)
        {
            a.AssignObject(runtime, BitAndScalarScalar(runtime, a, b));

            return a;
        }

        public static object BitAndScalarScalar(Runtime runtime,
                                                P5Scalar a, P5Scalar b)
        {
            if (a.IsString(runtime) && b.IsString(runtime))
            {
                string sa = a.AsString(runtime), sb = b.AsString(runtime);
                System.Text.StringBuilder t;

                if (sa.Length > sb.Length)
                {
                    t = new System.Text.StringBuilder(sa);

                    for (int i = 0; i < sb.Length; ++i)
                        t[i] &= sb[i];
                }
                else
                {
                    t = new System.Text.StringBuilder(sb);

                    for (int i = 0; i < sa.Length; ++i)
                        t[i] &= sa[i];
                }

                return t.ToString();
            }
            else
            {
                // TODO take into account signed/unsigned?
                return a.AsInteger(runtime) & b.AsInteger(runtime);
            }
        }
    }
}
