using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldFor : OldStatement
{
    public OldSet Set { get; set;}
    public BinaryOperation Expr { get; set; }
    public OldStatement Statement { get; set; }
    public OldBlock ForBlock { get; set; }

    public OldFor(OldSet set, BinaryOperation expr, OldStatement statement, OldBlock block)
    {
        Set = set;
        Expr = expr;
        Statement = statement;
        ForBlock = block;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool expr = false;
        while (true)
        {
            Set.Run(ref Manager);
            var varexpr = Expr.Run(ref Manager);
            if (varexpr is OldBool)
            {
                expr = (bool)(varexpr as OldBool).Value;
            }
            if (expr)
            {
                ForBlock.Run(ref Manager);
            }
            Statement.Run(ref Manager);
        }
    }
}