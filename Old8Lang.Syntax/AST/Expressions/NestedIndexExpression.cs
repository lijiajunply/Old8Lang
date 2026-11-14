using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>任意基础表达式上的嵌套索引。</summary>
public sealed class NestedIndexExpression(LangExpression target, LangExpression index, SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>基础表达式。</summary>
    public LangExpression Target { get; } = target;
    /// <summary>索引表达式。</summary>
    public LangExpression Index { get; } = index;
    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitNestedIndexExpression(this);
    /// <inheritdoc />
    public override string ToString() => $"{Target}[{Index}]";
}
