using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// if语句
/// </summary>
public class OldIf_Elif_Else : OldStatement
{
    public OldIf IfBlock { get; set; }
    public List<OldIf> ElifBlock { get; set; }
    public OldBlock ElseBlock { get; set; }

    public OldIf_Elif_Else(OldIf ifBlock, List<OldIf>? elifBlock, OldBlock elseBlock)
    {
        IfBlock = ifBlock;
        ElifBlock = elifBlock;
        ElseBlock = elseBlock;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool r = true;
        IfBlock.Run(ref Manager,ref r);
        if (ElifBlock is not null)
        {
            foreach (var VARIABLE in ElifBlock)
            {
                VARIABLE.Run(ref Manager,ref r);
            }
        }
        ElseBlock.Run(ref Manager);
    }
}