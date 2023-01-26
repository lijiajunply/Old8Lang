using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class FuncInit : OldStatement
{
    public FuncValue FuncValue { get; set; }

    public FuncInit(FuncValue a)
    {
        FuncValue = a;
    }

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddClassAndFunc(FuncValue.Id, FuncValue);
    }

    public override string ToString() => FuncValue.ToString();
}