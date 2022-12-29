using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldFunc : OldValue
{
    public OldID ID { get; set; }
    public OldBlock Block { get; set; }
    
    public OldExpr Return { get; set; }

    public OldFunc(OldID id, OldBlock block)
    {
        ID = id;
        Block = block;
    }

    public override OldExpr Run(ref VariateManager Manager)
    {
        Block.Run(ref Manager);
        return new OldValue();
    }
}