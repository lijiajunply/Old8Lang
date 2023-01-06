using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldFuncInit : OldStatement
{
    public OldFunc Func { get; set; }

    public OldFuncInit(OldFunc a)
    {
        Func = a;
    }

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddClassAndFunc(Func.ID, Func);
    }
}