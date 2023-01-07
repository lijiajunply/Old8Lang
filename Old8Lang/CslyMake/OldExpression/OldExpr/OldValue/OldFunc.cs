using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldFunc : OldValue
{
    public OldID ID { get; set; }
    public OldBlock Block { get; set; }
    public List<OldID> IDs { get; set; }
    public OldExpr Return { get; set; }

    public OldFunc(OldID id,List<OldID>ids,OldBlock block,OldExpr _return)
    {
        ID = id;
        IDs = ids;
        Block = block;
        Return = _return;
    }

    public override OldExpr Run(ref VariateManager Manager)
    {
        Manager.AddChildren();
        Block.Run(ref Manager);
        var a = Return as BinaryOperation;
        var b = a.Run(ref Manager);
        Manager.RemoveChildren();
        return b as OldValue;
    }
}