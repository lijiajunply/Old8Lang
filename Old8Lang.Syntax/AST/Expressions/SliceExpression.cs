using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>标识符上的切片表达式。</summary>
public sealed class SliceExpression(LangId id, LangExpression? start = null, LangExpression? end = null,
    LangExpression? step = null) : LangExpression
{
    /// <summary>集合标识符。</summary>
    public LangId Id { get; } = id;
    /// <summary>起始索引。</summary>
    public LangExpression? Start { get; } = start;
    /// <summary>结束索引。</summary>
    public LangExpression? End { get; } = end;
    /// <summary>步长。</summary>
    public LangExpression? Step { get; } = step;

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitSliceExpression(this);

    /// <inheritdoc />
    public override string ToString()
    {
        if (Start is not null && End is not null && Step is not null) return $"{Id}[{Start}:{End}:{Step}]";
        if (Start is not null && End is not null) return $"{Id}[{Start}:{End}]";
        if (Start is not null) return $"{Id}[{Start}:]";
        if (End is not null) return $"{Id}[:{End}]";
        return $"{Id}[:]";
    }
}
