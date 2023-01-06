using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldFunc : OldValue
{
    public OldID ID { get; set; }
    public OldBlock Block { get; set; }
    
    public OldExpr Return { get; set; }

    public OldFunc(OldID id, OldBlock block,OldExpr _return)
    {
        ID = id;
        Block = block;
        Return = _return;
    }

    public override OldExpr Run(ref VariateManager Manager)
    {
        Block.Run(ref Manager);
        var a = Return as BinaryOperation;
        return a.Run(ref Manager);
    }
}