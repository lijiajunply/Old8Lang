using Old8Lang.AST.Expression;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class DirectStatement : OldStatement
{
    public OldID ID1 { get; set; }
    public OldID ID2 { get; set; }
    public DirectStatement(OldID id1, OldID id2)
    {
        ID1 = id1;
        ID2 = id2;
    }

    public override void Run(ref VariateManager Manager)
    {
        Manager.Direct(ID1, ID2);
    }

    public override string ToString() => $"{ID1} direct {ID2}";
}