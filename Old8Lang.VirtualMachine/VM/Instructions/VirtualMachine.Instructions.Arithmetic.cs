using Old8Lang.Bytecode.Core;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行算术运算指令
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteArithmeticOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.Add:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.Push(Add(a, b));
            }
                break;

            case OpCode.Sub:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.Push(Sub(a, b));
            }
                break;

            case OpCode.Mul:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.Push(Mul(a, b));
            }
                break;

            case OpCode.Div:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.Push(Div(a, b));
            }
                break;

            case OpCode.Mod:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.Push(Mod(a, b));
            }
                break;

            case OpCode.Pow:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                Stack.Push(Pow(a, b));
            }
                break;

            case OpCode.Neg:
            {
                var a = Stack.Pop();
                Stack.Push(Neg(a));
            }
                break;

        }
    }
}
