using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ReturnStatement(OldExpr returnExpr) : OldStatement
{
    private OldExpr ReturnExpr { get; } = returnExpr;

    public override void Run(ref VariateManager Manager)
    {
        Manager.Result = ReturnExpr.Run(ref Manager);
        Manager.IsReturn = true;
    }

    public override string ToString() => $"return {ReturnExpr}";
}