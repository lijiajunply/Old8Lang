using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// return 语句。
/// </summary>
public partial class ReturnStatement(LangExpression returnExpression, SourcePosition position = default)
    : OldStatement(position)
{
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitReturnStatement(this);

    /// <summary>返回值表达式。</summary>
    public LangExpression Expression => returnExpression;

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override string ToString() => $"return {returnExpression}";
}
