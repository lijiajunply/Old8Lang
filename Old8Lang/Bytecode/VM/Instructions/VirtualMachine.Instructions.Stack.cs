using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行栈操作指令
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                // 被闭包按引用捕获的局部变量槽位里放的是共享单元，读取时解引用
                var localValue = frame.Locals[localIndex];
                _stack.Push(localValue is UpValueCell cell ? cell.Value : localValue);
            }
                break;

            case OpCode.StoreLocal:
            {
                int localIndex = (int)instruction.Operand!;
                var localValue = _stack.Pop();
                // 已装箱的槽位要写进共享单元，而不是把盒子本身替换掉，
                // 否则闭包持有的盒子会与外层脱离，写回失效
                if (frame.Locals[localIndex] is UpValueCell cell)
                {
                    cell.Value = localValue;
                }
                else
                {
                    frame.Locals[localIndex] = localValue;
                }
            }
                break;

            case OpCode.RefreshLocalBinding:
            {
                // 循环变量每轮迭代换一个新的共享单元：已经装箱时换成新盒子，
                // 此前创建的闭包继续持有旧盒子（各自保留当轮的值）；未装箱则无事发生。
                int localIndex = (int)instruction.Operand!;
                if (frame.Locals[localIndex] is UpValueCell existing)
                {
                    frame.Locals[localIndex] = new UpValueCell(existing.Value);
                }
            }
                break;

            case OpCode.LoadGlobal:
            {
                string varName = (string)instruction.Operand!;
                _stack.Push(ResolveGlobalValue(frame, varName, instruction));
            }
                break;

            case OpCode.StoreGlobal:
            {
                string varName = (string)instruction.Operand!;
                var globalValue = _stack.Pop();
                // 被闭包捕获的名字写进共享单元；否则写全局表
                if (frame.ClosureEnvironment == null ||
                    !frame.ClosureEnvironment.TrySetValue(varName, globalValue))
                {
                    _globals[varName] = globalValue;
                }
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
