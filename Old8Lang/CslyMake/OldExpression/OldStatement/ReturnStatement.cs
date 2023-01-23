using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class ReturnStatement : OldStatement
{
    public OldExpr ReturnExpr { get; set; }
    public ReturnStatement(OldExpr returnExpr) => ReturnExpr = returnExpr;
    public override void Run(ref VariateManager Manager)
    {
        Manager.Result   = ReturnExpr.Run(ref Manager);
        Manager.IsReturn = true;
    }
    public override string ToString() => $"return {ReturnExpr}";
}