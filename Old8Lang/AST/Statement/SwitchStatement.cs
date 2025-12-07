using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;


namespace Old8Lang.AST.Statement;

public class SwitchStatement(
    OldExpr switchExpr,
    List<OldCase> switchCaseList,
    BlockStatement? defaultBlockStatement = null)
    : OldStatement
{
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();
        var switchValue = switchExpr.Run(manager);

        foreach (var oldCase in from oldCase in switchCaseList
                 let caseValue = oldCase.Expr.Run(manager)
                 where switchValue.Equal(caseValue)
                 select oldCase)
        {
            oldCase.BlockStatement.Run(manager);
            manager.RemoveChildren();
            return;
        }

        defaultBlockStatement?.Run(manager);
        manager.RemoveChildren();
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var labelEnd = ilGenerator.DefineLabel();

        if (defaultBlockStatement != null)
        {
            defaultBlockStatement.GenerateIl(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, labelEnd); // 跳转到结束标签   
        }
        
        foreach (var oldCase in switchCaseList)
        {
            oldCase.GenerateIl(ilGenerator, local);
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

    public override void Run(VariateManager manager)
    {
        BlockStatement.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var labelCase = ilGenerator.DefineLabel();
        Expr.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Br, labelCase);

        ilGenerator.MarkLabel(labelCase);
        BlockStatement.GenerateIl(ilGenerator, local);
    }

    public override OldStatement this[int index] => BlockStatement[index];

    public override int Count => BlockStatement.Count;
}