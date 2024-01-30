using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// if语句
/// </summary>
public class IfStatement(OldIf ifBlock, List<OldIf?> elifBlock, BlockStatement? elseBlockStatement)
    : OldStatement
{
    private OldIf IfBlock { get; set; } = ifBlock;

    private List<OldIf?> ElifBlock { get; set; } = elifBlock;

    private BlockStatement? ElseBlockStatement { get; set; } = elseBlockStatement;

    public override void Run(ref VariateManager Manager)
    {
        var r = true;
        Manager.AddChildren();
        IfBlock.Run(ref Manager, ref r);
        Manager.RemoveChildren();
        foreach (var variable in ElifBlock)
        {
            if(variable is null)continue;
            Manager.AddChildren();
            variable.Run(ref Manager, ref r);
            Manager.RemoveChildren();
        }

        if (r)
            ElseBlockStatement?.Run(ref Manager);
        
    }

    public override string ToString() =>
        $"if({IfBlock} else if{Apis.ListToString(ElifBlock)} \nelse: {ElseBlockStatement}";
}