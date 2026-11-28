using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Core;
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
                // 优先使用调用帧的常量池（用于模块导入的函数）
                var constantPool = frame.ConstantPool ?? _bytecodeFile.ConstantPool;
                Stack.Push(constantPool.GetConstantValue(instruction.IntOperand));
            }
                break;

            case OpCode.LoadLocal:
            {
                // 被闭包按引用捕获的局部变量槽位里放的是共享单元，读取时解引用
                var localValue = frame.Locals[instruction.IntOperand];
                Stack.Push(localValue.Kind == VmValueKind.UpValue ? localValue.AsUpValue.Value : localValue);
            }
                break;

            case OpCode.StoreLocal:
            {
                int localIndex = instruction.IntOperand;
                var localValue = Stack.Pop();
                // 已装箱的槽位要写进共享单元，而不是把盒子本身替换掉，
                // 否则闭包持有的盒子会与外层脱离，写回失效
                if (frame.Locals[localIndex].Kind == VmValueKind.UpValue)
                {
                    frame.Locals[localIndex].AsUpValue.Value = localValue;
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
                int localIndex = instruction.IntOperand;
                if (frame.Locals[localIndex].Kind == VmValueKind.UpValue)
                {
                    frame.Locals[localIndex] = VmValue.FromUpValue(new UpValueCell(frame.Locals[localIndex].AsUpValue.Value));
                }
            }
                break;

            case OpCode.LoadGlobal:
            {
                string varName = (string)instruction.Operand!;
                if (frame.ClosureEnvironment is { IsEmpty: false } closureEnv)
                {
                    Stack.Push(ResolveGlobalVmValue(closureEnv, varName, instruction));
                    break;
                }

                var slot = instruction.CachedGlobalSlot;
                if (slot == null || slot.Owner != _globalsOwnerId)
                {
                    slot = ResolveGlobalSlot(varName, instruction);
                    instruction.CachedGlobalSlot = slot;
                }

                Stack.Push(slot.GetValue());
            }
                break;

            case OpCode.StoreGlobal:
            {
                string varName = (string)instruction.Operand!;
                var globalValue = Stack.Pop();
                // 被闭包捕获的名字写进共享单元；否则写全局表
                if (frame.ClosureEnvironment is null or { IsEmpty: true })
                {
                    var slot = instruction.CachedGlobalSlot;
                    if (slot == null || slot.Owner != _globalsOwnerId)
                    {
                        slot = GetOrCreateGlobalSlot(varName);
                        instruction.CachedGlobalSlot = slot;
                    }

                    slot.SetValue(globalValue);
                }
                else if (!frame.ClosureEnvironment.TrySetVmValue(varName, globalValue))
                {
                    SetGlobalVmValue(varName, globalValue);
                }
            }
                break;

            case OpCode.Pop:
                Stack.PopDiscard();
                break;

            case OpCode.Dup:
                Stack.Dup();
                break;

            case OpCode.LoadNull:
                Stack.PushNull();
                break;

            case OpCode.LoadTrue:
                Stack.PushBool(true);
                break;

            case OpCode.LoadFalse:
                Stack.PushBool(false);
                break;

            case OpCode.Swap:
                Stack.Swap();
                break;
        }
    }
}
