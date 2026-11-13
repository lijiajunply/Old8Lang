using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// break语句
/// </summary>
public partial class BreakStatement(SourcePosition position = default) : OldStatement(position)
{

    public override OldStatement this[int index] => throw new InvalidOperationError(new SourcePosition(),
        "Indexer not implemented for BreakStatement", "BreakStatement不支持索引访问");

    public override int Count => 0;

    public override string ToString() => "break";
}
