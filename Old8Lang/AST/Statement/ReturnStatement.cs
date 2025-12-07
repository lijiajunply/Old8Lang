using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang;

namespace Old8Lang.AST.Statement;

public class ReturnStatement(OldExpr returnExpr, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        manager.IsReturn = true;
        manager.Result = returnExpr.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        returnExpr.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ret);
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public Type OutputType(LocalManager local) => returnExpr.OutputType(local)!;

    public override string ToString() => $"return {returnExpr};";
}