using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class FuncInit : OldStatement
{
    public OldFunc Func { get; set; }

    public FuncInit(OldFunc a)
    {
        Func = a;
    }

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddClassAndFunc(Func.Id, Func);
    }

    public override string ToString() => Func.ToString();
}