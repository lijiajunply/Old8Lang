using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Statement;

public class SwitchStatement(
    OldExpr switchExpr,
    List<OldCase> switchCaseList,
    BlockStatement? defaultBlockStatement = null,
    SourcePosition position = default)
    : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();
        var switchValue = switchExpr.Run(manager);

        foreach (var oldCase in switchCaseList)
        {
            var caseValue = oldCase.Expr.Run(manager);
            bool isMatch = false;
            
            // 处理范围匹配：如果 caseValue 是数组，检查 switchValue 是否在数组中
            if (caseValue is ArrayLangValue arrayValue)
            {
                isMatch = arrayValue.GetItems().Any(item => switchValue.Equal(item));
            }
            // 普通相等匹配
            else
            {
                isMatch = switchValue.Equal(caseValue);
            }
            
            if (isMatch)
            {
                oldCase.BlockStatement.Run(manager);
                manager.RemoveChildren();
                return;
            }
        }

        defaultBlockStatement?.Run(manager);
        manager.RemoveChildren();
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var labelEnd = ilGenerator.DefineLabel();
        var labelDefault = defaultBlockStatement != null ? ilGenerator.DefineLabel() : labelEnd;
        
        // 处理 default 块
        if (defaultBlockStatement != null)
        {
            defaultBlockStatement.GenerateIl(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, labelEnd);
        }
        
        // 处理所有 case
        foreach (var oldCase in switchCaseList)
        {
            oldCase.BlockStatement.GenerateIl(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, labelEnd);
        }
        
        ilGenerator.MarkLabel(labelEnd);
    }

    public override OldStatement this[int index] => switchCaseList[index];

    public override int Count => switchCaseList.Count;
}

public class OldCase(OldExpr expr, BlockStatement blockStatement, SourcePosition position = default) : OldStatement(position)
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