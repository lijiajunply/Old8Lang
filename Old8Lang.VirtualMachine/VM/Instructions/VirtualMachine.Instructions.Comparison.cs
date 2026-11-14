using Old8Lang.Bytecode.Core;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行比较运算指令
    /// </summary>
    private void ExecuteComparisonOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.Equal:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(VmBoxed.Bool(Equals(a, b)));
            }
                break;

            case OpCode.NotEqual:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(VmBoxed.Bool(!Equals(a, b)));
            }
                break;

            case OpCode.Greater:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(VmBoxed.Bool(Greater(a, b)));
            }
                break;

            case OpCode.Less:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(VmBoxed.Bool(Less(a, b)));
            }
                break;

            case OpCode.GreaterEqual:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(VmBoxed.Bool(GreaterEqual(a, b)));
            }
                break;

            case OpCode.LessEqual:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(VmBoxed.Bool(LessEqual(a, b)));
            }
                break;

        }
    }
}
