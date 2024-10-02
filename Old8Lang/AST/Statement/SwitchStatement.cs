using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class SwitchStatement(
    OldExpr switchExpr,
    List<OldCase> switchCaseList,
    BlockStatement? defaultBlockStatement = null)
    : OldStatement
{
    private OldExpr SwitchExpr { get; } = switchExpr;
    private List<OldCase> SwitchCaseList { get; } = switchCaseList;
    private BlockStatement? DefaultBlockStatement { get; } = defaultBlockStatement;

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddChildren();
        var switchValue = SwitchExpr.Run(ref Manager);

        foreach (var oldCase in SwitchCaseList)
        {
            var caseValue = oldCase.Expr.Run(ref Manager);
            if (!switchValue.Equal(caseValue)) continue;
            oldCase.BlockStatement.Run(ref Manager);
            Manager.RemoveChildren();
            return;
        }

        DefaultBlockStatement?.Run(ref Manager);
        Manager.RemoveChildren();
    }
}

public class OldCase(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    public OldExpr Expr { get; } = expr;
    public BlockStatement BlockStatement { get; } = blockStatement;

    public override void Run(ref VariateManager Manager)
    {
        BlockStatement.Run(ref Manager);
    }
}