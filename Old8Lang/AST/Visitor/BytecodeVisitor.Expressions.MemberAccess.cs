using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 成员访问
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitClassMemberId(ClassMemberId node)
    {
        // ClassMemberId 是带访问修饰符的成员ID
        // 在字节码层面，它和普通的 LangId 类似
        // 加载成员的值
        string varName = node.IdName;

        // 检查是否是局部变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.LoadLocal, localIndex);
        }
        else
        {
            // 检查是否在类方法中，且 this 是局部变量（实例方法）
            // 如果是，则应该通过 this.field 访问字段
            if (_compiler.IsLocalVariable("this"))
            {
                // 这是一个实例方法中的字段访问
                // 加载 this
                int thisIndex = _compiler.GetLocalIndex("this");
                Emit(OpCode.LoadLocal, thisIndex);

                // 加载字段
                Emit(OpCode.GetField, varName);
            }
            else
            {
                // 全局变量或静态成员
                Emit(OpCode.LoadGlobal, varName);
            }
        }

        return null;
    }


}
