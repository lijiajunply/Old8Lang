using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// if语句
/// </summary>
public class IfStatement(OldIf ifBlock, List<OldIf?> elifBlock, BlockStatement? elseBlockStatement)
    : OldStatement
{
    public override void Run(ref VariateManager Manager)
    {
        var r = true;
        Manager.AddChildren();
        ifBlock.Run(ref Manager, ref r);
        Manager.RemoveChildren();
        foreach (var variable in elifBlock)
        {
            if(variable is null)continue;
            Manager.AddChildren();
            variable.Run(ref Manager, ref r);
            Manager.RemoveChildren();
        }

        if (r)
            elseBlockStatement?.Run(ref Manager);
        
    }

    public override string ToString() =>
        $"if({ifBlock} else if{Apis.ListToString(elifBlock)} \nelse: {elseBlockStatement}";
}