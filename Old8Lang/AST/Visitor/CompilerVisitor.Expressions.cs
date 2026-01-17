using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;

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
        // 委托给 LangId.LoadIlValue，确保逻辑一致
        node.LoadIlValue(ilGenerator, local);
        return null;
    }

    /// <summary>
    /// 访问 MatchExpression 节点（编译器）
    /// </summary>
    public object? VisitMatchExpression(MatchExpression node)
    {
        // 迁移自 MatchExpression.LoadIlValue()
        // MatchExpression 的逻辑已经封装在其 LoadIlValue 方法中，直接调用
        node.LoadIlValue(ilGenerator, local);
        return null;
    }
}
