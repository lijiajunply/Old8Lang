using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// continue语句
/// </summary>
public partial class ContinueStatement(SourcePosition position = default) : OldStatement(position)
{

    public override OldStatement this[int index] => throw new InvalidOperationError(new SourcePosition(),
        "Indexer not implemented for ContinueStatement", "ContinueStatement不支持索引访问");

    public override int Count => 0;

    public override string ToString() => "continue";
}
