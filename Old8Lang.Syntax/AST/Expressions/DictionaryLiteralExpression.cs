using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>字典字面量的键值语法，不保存运行时字典状态。</summary>
public sealed class DictionaryLiteralExpression(
    IReadOnlyList<KeyValuePair<LangExpression, LangExpression>> entries,
    SourcePosition position = default) : LangExpression(position)
{
    /// <summary>按源码顺序排列的键值表达式。</summary>
    public IReadOnlyList<KeyValuePair<LangExpression, LangExpression>> Entries { get; } = entries.ToArray();

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitDictionaryLiteralExpression(this);

    /// <inheritdoc />
    public override string ToString() => $"{{{string.Join(", ", Entries.Select(entry => $"{entry.Key}: {entry.Value}"))}}}";
}
