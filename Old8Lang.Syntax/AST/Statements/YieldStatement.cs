using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// yield 语句，用于生成器函数中暂停执行并返回值。
/// </summary>
public partial class YieldStatement(LangExpression yieldExpression, SourcePosition position = default)
    : OldStatement(position)
{
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitYieldStatement(this);

    /// <summary>yield 表达式。</summary>
    public LangExpression YieldExpression { get; init; } = yieldExpression;

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override string ToString() => $"yield {YieldExpression}";
}
