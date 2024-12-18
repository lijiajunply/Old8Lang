using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ReturnStatement(OldExpr returnExpr) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        Manager.IsReturn = true;
        Manager.Result = returnExpr.Run(Manager);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        returnExpr.LoadILValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ret);
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public Type OutputType(LocalManager local) => returnExpr.OutputType(local)!;

    public override string ToString() => $"return {returnExpr};";
}