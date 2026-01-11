using System.Reflection.Emit;
using Old8Lang.AST.Expression;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Expression 节点的 Visit 方法实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 LangId 节点
    /// </summary>
    public object? VisitLangId(LangId node)
    {
        // 迁移自 LangId.LoadIlValue()
        var value = local.GetLocalVar(node.IdName);
        if (value is null)
        {
            // 检查是否是函数参数
            // 函数参数是通过Ldarg指令访问的，而不是Ldloc指令
            ilGenerator.Emit(OpCodes.Ldarg_0); // 假设只有一个参数，索引为0
        }
        else
        {
            ilGenerator.Emit(OpCodes.Ldloc, value);
        }
        return null;
    }
}
