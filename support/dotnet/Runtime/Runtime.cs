using org.mbarbon.p.values;
using System.Collections.Generic;

namespace org.mbarbon.p.runtime
{
    public struct SavedLexState
    {
        public string Package;
        public int Hints;
    }

    public struct SavedValue
    {
        public IP5Any container;
        public IP5Any value;
        public int int_key;
        public string str_key;
    }

    public struct StackFrame
    {
        public StackFrame(string pack, string file, int l, P5Code code,
                          Opcode.ContextValues cxt, bool eval)
        {
            Package = pack;
            File = file;
            Line = l;
            Code = code;
            Context = cxt;
            IsEval = eval;
        }

        public string Package;
        public string File;
        public int Line;
        public P5Code Code;
        public Opcode.ContextValues Context;
        public bool IsEval;
    }

    public partial class Runtime
    {
        public static System.Guid PerlGuid3 =
            new System.Guid("3FC48569-0551-4114-BF17-735ED691526B");
        private readonly string[] pathsep = new string[] {":"};

        public Runtime()
        {
            SymbolTable = new P5MainSymbolTable(this, "main");
            CallStack = new Stack<StackFrame>();

            // set up INC
            ModuleLoaders = new List<IModuleLoader>();
            ModuleLoaders.Add(new IncModuleLoader());

            var p5lib = System.Environment.GetEnvironmentVariable("PERL5LIB");
            var inc = SymbolTable.GetArray(this, "INC", true);

            if (p5lib != null)
                foreach (var dir in p5lib.Split(pathsep, System.StringSplitOptions.None))
                    inc.Push(this, new P5Scalar(this, dir));

            inc.Push(this, new P5Scalar(this, "."));
        }

        public void SetException(P5Exception e)
        {
            P5Scalar s = e.Reference;

            if (s == null)
                s = new P5Scalar(this, e.Message);

            SymbolTable.GetStashScalar(this, "@", true).Assign(this, s);
        }

        public IP5Any CallerNoArg(Opcode.ContextValues cxt)
        {
            return Caller(true, 0, cxt);
        }

        public IP5Any CallerWithArg(object level, Opcode.ContextValues cxt)
        {
            return Caller(false, Builtins.ConvertToInteger(this, level), cxt);
        }

        private IP5Any Caller(bool noarg, int level, Opcode.ContextValues cxt)
        {
            if (level >= CallStack.Count)
            {
                if (cxt == Opcode.ContextValues.SCALAR)
                    return new P5Scalar(this);
                else
                    return new P5List(this);
            }

            StackFrame frame;

            if (level == 0)
                frame = CallStack.Peek();
            else
            {
                frame = new StackFrame();

                foreach (var f in CallStack)
                {
                    frame = f;

                    if (level == 0)
                        break;
                    --level;
                }
            }

            if (cxt == Opcode.ContextValues.SCALAR)
                return new P5Scalar(this, frame.Package);
            else if (noarg)
                return new P5List(
                    this,
                    new P5Scalar(this, frame.Package),
                    new P5Scalar(this, frame.File),
                    new P5Scalar(this, frame.Line));
            else
            {
                var callcxt =
                    frame.Context == Opcode.ContextValues.VOID   ? new P5Scalar(this) :
                    frame.Context == Opcode.ContextValues.SCALAR ? new P5Scalar(this, "") :
                                                                   new P5Scalar(this, 1);
                var subname = frame.Code != null ?
                    new P5Scalar(this, frame.Code.Name) : new P5Scalar(this);

                return new P5List(
                    this,
                    new P5Scalar(this, frame.Package),
                    new P5Scalar(this, frame.File),
                    new P5Scalar(this, frame.Line),
                    subname,
                    new P5Scalar(this), // hasargs
                    callcxt, // context
                    new P5Scalar(this), // evaltext
                    new P5Scalar(this), // is_require
                    new P5Scalar(this), // hints
                    new P5Scalar(this)); // warnings
            }
        }

        public Opcode.ContextValues CurrentContext()
        {
            return CallStack.Peek().Context;
        }

        public P5MainSymbolTable SymbolTable;
        public List<IModuleLoader> ModuleLoaders;
        public Stack<StackFrame> CallStack;
        public string File, Package;
        public int Line, Hints;
        public RxResult LastMatch;
        // TODO add more generic runtime/generator options
        public bool CompileOnly;
        public bool NativeRegex, IgnoreBytecode;
    }
}
