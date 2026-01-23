using Old8Lang.Bytecode.Core;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行算术运算指令
    /// </summary>
    private void ExecuteArithmeticOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.Add:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Add(a, b));
            }
                break;

            case OpCode.Sub:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Sub(a, b));
            }
                break;

            case OpCode.Mul:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Mul(a, b));
            }
                break;

            case OpCode.Div:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Div(a, b));
            }
                break;

            case OpCode.Mod:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Mod(a, b));
            }
                break;

            case OpCode.Pow:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Pow(a, b));
            }
                break;

            case OpCode.Neg:
            {
                var a = _stack.Pop();
                _stack.Push(Neg(a));
            }
                break;

        }
    }
}
