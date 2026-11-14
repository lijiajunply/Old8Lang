using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

public partial class ThrowStatement(LangExpression expression, SourcePosition position = default) : OldStatement(position)
{
    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitThrowStatement(this);

    /// <summary>要抛出的表达式。</summary>
    public LangExpression Expression => expression;

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override string ToString() => $"throw {expression}";
}