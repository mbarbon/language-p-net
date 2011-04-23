using org.mbarbon.p.runtime;
using org.mbarbon.p.values;

namespace org.mbarbon.p.values
{
    public partial class P5Code : IP5Referrable
    {
        public static readonly int[] EMPTY_PROTO = new int[] { 0, 0, 0 };

        public P5Code(string _name, int[] _proto) :
            this(_name, _proto, null, false)
        {
            subref = new Sub(UndefinedSub);
        }

        public bool IsDefined(Runtime runtime)
        {
            return subref != (Sub)UndefinedSub;
        }

        public P5Code(string _name, int[] _proto,
                      System.Delegate code, bool main)
        {
            subref = (Sub)code;
            scratchpad = null;
            is_main = main;
            name = _name;
            proto = _proto;
        }

        public P5Code(string _name, System.Delegate code,
                      object _value, int _flags) :
            this(_name, EMPTY_PROTO, code, false)
        {
            const_value = _value;
            const_flags = _flags;
        }

        public void Assign(Runtime runtime, P5Code other)
        {
            subref = other.subref;
            proto = other.proto;
            scratchpad = other.scratchpad;
            const_value = other.const_value;
            const_flags = other.const_flags;
        }

        private IP5Any UndefinedSub(Runtime runtime,
                                    Opcode.ContextValues context,
                                    P5ScratchPad pad, P5Array args)
        {
            var msg = string.Format("Undefined subroutine &{0:S} called",
                                    Name);

            throw new P5Exception(runtime, msg);
        }

        public virtual IP5Any Call(Runtime runtime, Opcode.ContextValues context,
                                   P5Array args)
        {
            // TODO emit this in the subroutine prologue/epilogue code,
            //      as is done for eval BLOCK
            P5ScratchPad pad = scratchpad;
            if (scratchpad != null && !is_main)
                pad = scratchpad.NewScope(runtime);
            int size = runtime.CallStack.Count;

            try
            {
                runtime.CallStack.Push(new StackFrame(runtime.Package,
                                                      runtime.File,
                                                      runtime.Line, this,
                                                      context, false));
                return subref(runtime, context, pad, args);
            }
            finally
            {
                StackFrame frame = null;
                while (runtime.CallStack.Count > size)
                    frame = runtime.CallStack.Pop();

                if (frame != null)
                {
                    runtime.Package = frame.Package;
                    runtime.File = frame.File;
                    runtime.Line = frame.Line;
                }
            }
        }

        // P5MainCode and P5BeginCode subclasses?
        public virtual IP5Any CallMain(Runtime runtime)
        {
            return subref(runtime, Opcode.ContextValues.VOID, scratchpad, null);
        }

        public void NewScope(Runtime runtime)
        {
            if (scratchpad != null)
                scratchpad = scratchpad.NewScope(runtime);
        }

        public virtual P5Scalar MakeClosure(Runtime runtime, P5ScratchPad outer)
        {
            P5Code closure = new P5Code(name, proto, subref, is_main);
            closure.scratchpad = scratchpad.CloseOver(runtime, outer);

            if (const_flags == 0)
                return new P5Scalar(runtime, closure);

            // constant sub optimization
            var value = closure.scratchpad.GetScalar(runtime, 0) as P5Scalar;
            if (value == null)
                return new P5Scalar(runtime, closure);

            var body = value.Body as P5StringNumber;
            if (body == null)
                return new P5Scalar(runtime, closure);

            int constant_flags = 0;
            object constant_value = null;

            if (body.IsString(runtime))
            {
                constant_value = body.AsString(runtime);
                constant_flags = 1; // CONST_STRING
            }
            else if (body.IsInteger(runtime))
            {
                constant_value = body.AsInteger(runtime);
                constant_flags = 10; // CONST_NUMBER|NUM_INTEGER
            }
            else if (body.IsFloat(runtime))
            {
                constant_value = body.AsFloat(runtime);
                constant_flags = 18; // CONST_NUMBER|NUM_FLOAT
            }

            if (constant_flags != 0)
            {
                var const_sub = new P5Code(null, subref,
                                           constant_value, constant_flags);
                const_sub.scratchpad = closure.scratchpad;

                closure = const_sub;
            }

            return new P5Scalar(runtime, closure);
        }

        public virtual void Bless(Runtime runtime, P5SymbolTable stash)
        {
            blessed = stash;
        }

        public virtual bool IsBlessed(Runtime runtime)
        {
            return blessed != null;
        }

        public virtual P5SymbolTable Blessed(Runtime runtime)
        {
            return blessed;
        }

        public virtual string ReferenceTypeString(Runtime runtime)
        {
            return "CODE";
        }

        public P5ScratchPad ScratchPad
        {
            get { return scratchpad; }
            set { scratchpad = value; }
        }

        public string Name
        {
            get { return name.IndexOf("::") == -1 ? "main::" + name : name; }
        }

        public int[] Prototype { get { return proto; } }

        protected Sub Subref { get { return subref; } }

        public delegate IP5Any Sub(Runtime runtime,
                                   Opcode.ContextValues context,
                                   P5ScratchPad pad, P5Array args);

        private P5SymbolTable blessed;
        private Sub subref;
        private P5ScratchPad scratchpad;
        private bool is_main;
        private string name;
        private int[] proto;
        private object const_value;
        private int const_flags;
    }

    public class P5NativeCode : P5Code
    {
        public P5NativeCode(string name, System.Delegate code) :
            base(name, null, code, false)
        {
        }

        public override IP5Any Call(Runtime runtime, Opcode.ContextValues context,
                                    P5Array args)
        {
            int size = runtime.CallStack.Count;

            try
            {
                runtime.CallStack.Push(new StackFrame(runtime.Package,
                                                      runtime.File,
                                                      runtime.Line, this,
                                                      context, false));
                return Subref(runtime, context, ScratchPad, args);
            }
            finally
            {
                while (runtime.CallStack.Count > size)
                    runtime.CallStack.Pop();
            }
        }
    }
}
