using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;
/// <summary>
/// if语句
/// </summary>
public class IfStatement : OldStatement
{
    public OldIf IfBlock { get; set; }
    public List<OldIf> ElifBlock { get; set; }
    public BlockStatement ElseBlockStatement { get; set; }

    public IfStatement(OldIf ifBlock, List<OldIf>? elifBlock, BlockStatement elseBlockStatement)
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

        if (ElseBlockStatement is not null && r)
        {
            ElseBlockStatement.Run(ref Manager);
        }
    }
    public override string ToString() => $"if: {IfBlock}elif: {APIs.ListToString(ElifBlock)} \nelse: {ElseBlockStatement}";
}