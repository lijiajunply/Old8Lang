using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>范围表达式的端点和包含选项。</summary>
public sealed class RangeLiteralExpression(
    LangExpression? start,
    LangExpression? end,
    SourcePosition position = default,
    bool includeStart = true,
    bool includeEnd = true,
    LangExpression? step = null) : LangExpression(position)
{
    /// <summary>起始表达式。</summary>
    public LangExpression? Start { get; } = start;
    /// <summary>结束表达式。</summary>
    public LangExpression? End { get; } = end;
    /// <summary>可选的步长表达式。</summary>
    public LangExpression? Step { get; } = step;
    /// <summary>是否包含起点。</summary>
    public bool IncludeStart { get; } = includeStart;
    /// <summary>是否包含终点。</summary>
    public bool IncludeEnd { get; } = includeEnd;

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitRangeLiteralExpression(this);

    /// <inheritdoc />
    public override string ToString() => $"{(IncludeStart ? "[" : "(")}{Start}~{End}{(Step is null ? "" : $":{Step}")}{(IncludeEnd ? "]" : ")")}";
}
