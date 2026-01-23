using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行栈操作指令
    /// </summary>
    private void ExecuteStackOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.Nop:
                // 无操作
                break;

            case OpCode.LoadConst:
            {
                int constIndex = (int)instruction.Operand!;
                // 优先使用调用帧的常量池（用于模块导入的函数）
                var constantPool = frame.ConstantPool ?? _bytecodeFile.ConstantPool;
                var constant = constantPool.GetConstant(constIndex);
                _stack.Push(constant);
            }
                break;

            case OpCode.LoadLocal:
            {
                int localIndex = (int)instruction.Operand!;
                _stack.Push(frame.Locals[localIndex]);
            }
                break;

            case OpCode.StoreLocal:
            {
                int localIndex = (int)instruction.Operand!;
                frame.Locals[localIndex] = _stack.Pop();
            }
                break;

            case OpCode.LoadGlobal:
            {
                string varName = (string)instruction.Operand!;

                // 先检查闭包环境
                if (frame.ClosureEnvironment != null &&
                    frame.ClosureEnvironment.TryGetValue(varName, out var closureValue))
                {
                    _stack.Push(closureValue);
                }
                // 再检查全局变量
                else if (_globals.TryGetValue(varName, out var globalValue))
                {
                    _stack.Push(globalValue);
                }
                else
                {
                    throw new NameError(GetPosition(instruction), varName);
                }
            }
                break;

            case OpCode.StoreGlobal:
            {
                string varName = (string)instruction.Operand!;
                _globals[varName] = _stack.Pop();
            }
                break;

            case OpCode.Pop:
                _stack.Pop();
                break;

            case OpCode.Dup:
                _stack.Push(_stack.Peek());
                break;

            case OpCode.LoadNull:
                _stack.Push(null);
                break;

            case OpCode.LoadTrue:
                _stack.Push(true);
                break;

            case OpCode.LoadFalse:
                _stack.Push(false);
                break;

            case OpCode.Swap:
            {
                var a = _stack.Pop();
                var b = _stack.Pop();
                _stack.Push(a);
                _stack.Push(b);
            }
                break;

        }
    }
}
