using Runtime = org.mbarbon.p.runtime.Runtime;

namespace org.mbarbon.p.values
{
    public class P5Exception : System.Exception
    {
        public P5Exception(Runtime runtime, string str)
        {
            message = str;
            file = runtime.File;
            line = runtime.Line;
        }

        public P5Exception(Runtime runtime, string str, string _file, int _line)
        {
            message = str;
            file = _file;
            line = _line;
        }

        public P5Exception(Runtime runtime, P5Scalar objref)
        {
            reference = objref;
        }

        public string AsString(Runtime runtime)
        {
            if (reference != null)
                return reference.AsString(runtime);

            return Message;
        }

        public override string Message
        {
            get
            {
                if (message == null)
                    return string.Format("Reference exception at {0:S} line {1}.\n",
                                         file, line);
                if (message.EndsWith("\n"))
                    return message;

                return string.Format("{0:S} at {1:S} line {2}.\n", message,
                                     file, line);
            }
        }

        public P5Scalar Reference { get { return reference; } }

        private P5Scalar reference;
        private string message, file;
        private int line;
    }
}
