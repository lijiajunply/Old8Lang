using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldDirect : OldStatement
{
    public OldID ID1 { get; set; }
    public OldID ID2 { get; set; }
    public OldDirect(OldID id1, OldID id2)
    {
        ID1 = id1;
        ID2 = id2;
    }

    public override void Run(ref VariateManager Manager)
    {
        Manager.Direct(ID1, ID2);
    }
}