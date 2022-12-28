namespace Old8Lang.CslyMake.OldExpression;

public class OldIf : OldCompound
{
    public OldExpr Expr { get; set; }
    public OldBlock Block { get; set; }

    public OldIf(OldExpr expr, OldBlock block)
    {
        Expr = expr;
        Block = block;
    }
}