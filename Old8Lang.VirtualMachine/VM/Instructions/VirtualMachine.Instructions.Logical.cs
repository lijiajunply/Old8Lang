using Old8Lang.Bytecode.Core;

// ReSharper disable once CheckNamespace
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
                var b = Stack.Pop();
                var a = Stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "&&", instruction);
                ValidateLogicalOperand(b, "&&", instruction);
                Stack.PushBool(ToBool(a) && ToBool(b));
            }
                break;

            case OpCode.Or:
            {
                var b = Stack.Pop();
                var a = Stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "||", instruction);
                ValidateLogicalOperand(b, "||", instruction);
                Stack.PushBool(ToBool(a) || ToBool(b));
            }
                break;

            case OpCode.Not:
            {
                var a = Stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "!", instruction);
                Stack.PushBool(!ToBool(a));
            }
                break;
        }
    }
}
