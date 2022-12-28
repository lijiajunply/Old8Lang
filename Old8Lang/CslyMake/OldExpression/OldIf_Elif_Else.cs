namespace Old8Lang.CslyMake.OldExpression;

public class OldIf_Elif_Else : OldCompound
{
    public OldIf IfBlock { get; set; }
    public OldIf ElifBlock { get; set; }
    public OldBlock ElseBlock { get; set; }

    public OldIf_Elif_Else(OldIf ifBlock, OldIf elifBlock, OldBlock elseBlock)
    {
        IfBlock = ifBlock;
        ElifBlock = elifBlock;
        ElseBlock = elseBlock;
    }
}