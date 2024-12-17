using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ReturnStatement(OldExpr returnExpr) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        Manager.Result = returnExpr.Run(Manager);
        Manager.IsReturn = true;
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }

    public override string ToString() => $"return {returnExpr};";
}