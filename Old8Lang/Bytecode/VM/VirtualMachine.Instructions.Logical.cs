using Old8Lang.Bytecode.Core;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行逻辑运算指令
    /// </summary>
    private void ExecuteLogicalOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.And:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "&&", instruction);
                ValidateLogicalOperand(b, "&&", instruction);
                _stack.Push(ToBool(a) && ToBool(b));
            }
                break;

            case OpCode.Or:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "||", instruction);
                ValidateLogicalOperand(b, "||", instruction);
                _stack.Push(ToBool(a) || ToBool(b));
            }
                break;

            case OpCode.Not:
            {
                var a = _stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "!", instruction);
                _stack.Push(!ToBool(a));
            }
                break;

        }
    }
}
