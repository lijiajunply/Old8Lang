using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldClassInit : OldStatement
{
    public OldAny _Class { get; set; }
    public OldClassInit(OldAny any)
    {
        _Class = any;
    }
    public override void Run(ref VariateManager Manager)
    {
        Manager.AddClassAndFunc(_Class.Id, _Class);
    }
}