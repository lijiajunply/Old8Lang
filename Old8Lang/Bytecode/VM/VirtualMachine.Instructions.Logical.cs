using System.Collections;
using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Bytecode.Interop;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

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
