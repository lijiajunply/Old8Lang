using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>数组字面量的元素语法，不保存运行时数组状态。</summary>
public sealed class ArrayLiteralExpression(IReadOnlyList<LangExpression> elements, SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>按源码顺序排列的元素表达式。</summary>
    public IReadOnlyList<LangExpression> Elements { get; } = elements.ToArray();

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitArrayLiteralExpression(this);

    /// <inheritdoc />
    public override string ToString() => $"[{string.Join(", ", Elements)}]";
}
