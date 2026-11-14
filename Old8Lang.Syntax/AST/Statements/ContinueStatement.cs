using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// continue 语句。
/// </summary>
public partial class ContinueStatement(SourcePosition position = default) : OldStatement(position)
{
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitContinueStatement(this);

    public override OldStatement this[int index] => throw new NotSupportedException("ContinueStatement 不支持索引访问");

    public override int Count => 0;

    public override string ToString() => "continue";
}
