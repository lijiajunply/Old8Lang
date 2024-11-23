using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class FuncInit(FuncValue a) : OldStatement
{
    public readonly FuncValue FuncValue = a;

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddClassAndFunc(FuncValue);
    }

    public override string ToString() => FuncValue.ToString();
}