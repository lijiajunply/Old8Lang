using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>标识符上的索引访问表达式。</summary>
public sealed class IndexExpression(LangId target, LangExpression key, SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>集合标识符。</summary>
    public LangId Target { get; } = target;
    /// <summary>索引或键表达式。</summary>
    public LangExpression Key { get; } = key;

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitIndexExpression(this);

    /// <inheritdoc />
    public override string ToString() => $"{Target}[{Key}]";
}
