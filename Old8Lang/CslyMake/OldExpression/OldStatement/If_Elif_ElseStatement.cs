using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// if语句
/// </summary>
public class If_Elif_ElseStatement : OldStatement
{
    public OldIf IfBlock { get; set; }
    public List<OldIf> ElifBlock { get; set; }
    public BlockStatement ElseBlockStatement { get; set; }

    public If_Elif_ElseStatement(OldIf ifBlock, List<OldIf>? elifBlock, BlockStatement elseBlockStatement)
    {
        IfBlock = ifBlock;
        ElifBlock = elifBlock;
        ElseBlockStatement = elseBlockStatement;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool r = true;
        Manager.AddChildren();
        IfBlock.Run(ref Manager,ref r);
        Manager.RemoveChildren();
        if (ElifBlock is not null)
        {
            foreach (var VARIABLE in ElifBlock)
            {
                Manager.AddChildren();
                VARIABLE.Run(ref Manager,ref r);
                Manager.RemoveChildren();
            }
        }

        if (ElseBlockStatement is not null)
        {
            ElseBlockStatement.Run(ref Manager);
        }
        
    }
}