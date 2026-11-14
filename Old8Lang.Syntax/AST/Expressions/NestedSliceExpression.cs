using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>任意基础表达式上的嵌套切片。</summary>
public sealed class NestedSliceExpression(LangExpression target, LangExpression start, LangExpression? end = null,
    LangExpression? step = null, SourcePosition position = default) : LangExpression(position)
{
    /// <summary>基础表达式。</summary>
    public LangExpression Target { get; } = target;
    /// <summary>起始表达式。</summary>
    public LangExpression Start { get; } = start;
    /// <summary>结束表达式。</summary>
    public LangExpression? End { get; } = end;
    /// <summary>步长表达式。</summary>
    public LangExpression? Step { get; } = step;
    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitNestedSliceExpression(this);
    /// <inheritdoc />
    public override string ToString()
    {
        var slice = End is not null
            ? (Step is not null ? $"{Start}:{End}:{Step}" : $"{Start}:{End}")
            : (Step is not null ? $"{Start}::{Step}" : $"{Start}:");
        return $"{Target}[{slice}]";
    }
}
