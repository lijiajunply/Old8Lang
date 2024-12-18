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

        foreach (var oldCase in from oldCase in switchCaseList
                 let caseValue = oldCase.Expr.Run(Manager)
                 where switchValue.Equal(caseValue)
                 select oldCase)
        {
            oldCase.BlockStatement.Run(Manager);
            Manager.RemoveChildren();
            return;
        }

        defaultBlockStatement?.Run(Manager);
        Manager.RemoveChildren();
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        var labelEnd = ilGenerator.DefineLabel();

        if (defaultBlockStatement != null)
        {
            defaultBlockStatement.GenerateIL(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, labelEnd); // 跳转到结束标签   
        }
        
        foreach (var oldCase in switchCaseList)
        {
            oldCase.GenerateIL(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, labelEnd);
        }
        ilGenerator.MarkLabel(labelEnd);
    }

    public override OldStatement this[int index] => switchCaseList[index];

    public override int Count => switchCaseList.Count;
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
        var labelCase = ilGenerator.DefineLabel();
        Expr.LoadILValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Br, labelCase);

        ilGenerator.MarkLabel(labelCase);
        BlockStatement.GenerateIL(ilGenerator, local);
    }

    public override OldStatement this[int index] => BlockStatement[index];

    public override int Count => BlockStatement.Count;
}