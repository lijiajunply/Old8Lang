using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldClassInit : OldStatement
{
    public OldAny Any { get; set; }
    public OldClassInit(OldAny any)
    {
        Any = any;
    }
    public override void Run(ref VariateManager Manager) => Manager.AddClassAndFunc(Any.Id,Any);

    public override string ToString() => Any.ToString();
}