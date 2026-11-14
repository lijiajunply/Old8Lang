using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// break 语句。
/// </summary>
public partial class BreakStatement(SourcePosition position = default) : OldStatement(position)
{
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitBreakStatement(this);

    public override OldStatement this[int index] => throw new NotSupportedException("BreakStatement 不支持索引访问");

    public override int Count => 0;

    public override string ToString() => "break";
}
