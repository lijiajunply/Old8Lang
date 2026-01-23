using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 基础表达式
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitLangId(LangId node)
    {
        string varName = node.IdName;

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.LoadLocal, localIndex);
        }
        // 检查是否是类名（优先于实例字段检查）
        else if (_compiler.IsClassName(varName))
        {
            // 这是一个类名，应该作为全局变量加载（类元数据）
            Emit(OpCode.LoadGlobal, varName);
        }
        // 检查是否是当前类的字段
        else if (_compiler.IsClassField(varName))
        {
            // 这是一个字段访问：this.field
            // 加载 this（第一个局部变量）
            Emit(OpCode.LoadLocal, 0);

            // 加载字段
            Emit(OpCode.GetField, varName);
        }
        else
        {
            // 全局变量
            Emit(OpCode.LoadGlobal, varName);
        }

        return null;
    }

}
