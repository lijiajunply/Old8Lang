using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>元组字面量或解构目标的语法，不保存运行时元组。</summary>
public sealed class TupleLiteralExpression(
    IReadOnlyList<LangExpression> elements,
    IReadOnlyList<string?>? fieldNames = null,
    SourcePosition position = default) : LangExpression(position)
{
    /// <summary>按源码顺序排列的元素表达式。</summary>
    public IReadOnlyList<LangExpression> Elements { get; } = elements.ToArray();

    /// <summary>可选字段名，未命名元素对应 null。</summary>
    public IReadOnlyList<string?>? FieldNames { get; } = fieldNames?.ToArray();

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitTupleLiteralExpression(this);

    /// <inheritdoc />
    public override string ToString() => $"({string.Join(", ", Elements)})";
}
