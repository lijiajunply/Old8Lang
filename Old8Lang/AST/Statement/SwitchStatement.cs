using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class SwitchStatement(
    OldExpr switchExpr,
    List<OldCase> switchCaseList,
    BlockStatement? defaultBlockStatement = null)
    : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        Manager.AddChildren();
        var switchValue = switchExpr.Run(Manager);

        foreach (var oldCase in switchCaseList)
        {
            var caseValue = oldCase.Expr.Run(Manager);
            if (!switchValue.Equal(caseValue)) continue;
            oldCase.BlockStatement.Run(Manager);
            Manager.RemoveChildren();
            return;
        }

        defaultBlockStatement?.Run(Manager);
        Manager.RemoveChildren();
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }
}

public class OldCase(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    public OldExpr Expr { get; } = expr;
    public BlockStatement BlockStatement { get; } = blockStatement;

    public override void Run(VariateManager Manager)
    {
        BlockStatement.Run(Manager);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }
}