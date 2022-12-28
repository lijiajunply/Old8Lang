using Old8Lang.CslyMake.OldLandParser;

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

    public override void Run(ref VariateManager Manager)
    {
        while (true)
        {
            Set.Run(ref Manager);
            bool expr = Expr.CompareRun(ref Manager);
            if (expr)
            {
                ForBlock.Run(ref Manager);
            }
            Statement.Run(ref Manager);
        }
    }
}