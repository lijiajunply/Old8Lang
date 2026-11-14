using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>列表字面量的元素语法，不保存可变的运行时列表。</summary>
public sealed class ListLiteralExpression(IReadOnlyList<LangExpression> elements, SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>按源码顺序排列的元素表达式。</summary>
    public IReadOnlyList<LangExpression> Elements { get; } = elements.ToArray();

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitListLiteralExpression(this);

    /// <inheritdoc />
    public override string ToString() => $"{{{string.Join(", ", Elements)}}}";
}
