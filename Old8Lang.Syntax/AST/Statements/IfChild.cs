using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// if / elif 分支：条件与语句块。
/// </summary>
public partial class IfChild(LangExpression expression, BlockStatement blockStatement, SourcePosition position = default)
    : OldStatement(position)
{
    public LangExpression Condition => expression;
    public BlockStatement Block => blockStatement;

    public override string ToString() => $"{expression}\n {{ {blockStatement} }}";

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("IfChild 是内部辅助类，不支持 Visitor 模式访问");
    }
}
