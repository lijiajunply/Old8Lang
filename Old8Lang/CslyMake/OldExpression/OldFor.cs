namespace Old8Lang.CslyMake.OldExpression;

public class OldFor : OldCompound
{
    public OldSet Set { get; set;}
    public OldExpr Expr { get; set; }
    public OldStatement Statement { get; set; }
    public OldBlock ForBlock { get; set; }

    public OldFor(OldSet set, OldExpr expr, OldStatement statement, OldBlock block)
    {
        Set = set;
        Expr = expr;
        Statement = statement;
        ForBlock = block;
    }
}