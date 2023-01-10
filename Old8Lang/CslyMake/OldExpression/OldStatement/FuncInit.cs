using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class FuncInit : OldStatement
{
    public OldFunc Func { get; set; }

    public FuncInit(OldFunc a)
    {
        Func = a;
    }

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddClassAndFunc(Func.ID, Func);
    }

    public override string ToString() => Func.ToString();
}