namespace Old8Lang.CslyMake.OldExpression;

public class OldWhile : OldCompound
{
    public OldExpr Expr { get; set; }
    public OldBlock Block { get; set; }

    public OldWhile(OldExpr expr, OldBlock block)
    {
        Expr = expr;
        Block = block;
    }
}