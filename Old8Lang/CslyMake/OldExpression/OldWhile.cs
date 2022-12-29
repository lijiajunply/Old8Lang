using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// while语句
/// </summary>
public class OldWhile : OldStatement
{
    public BinaryOperation Expr { get; set; }
    public OldBlock Block { get; set; }

    public OldWhile(BinaryOperation expr, OldBlock block)
    {
        Expr = expr;
        Block = block;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool expr = false;
        while (true)
        {
            var varbool = Expr.Run(ref Manager);
            if (varbool is OldBool)
            {
                expr = (bool)(Expr.Run(ref Manager) as OldBool).Value;
            }
            if (expr)
            {
                Block.Run(ref Manager);
            }
            else
            {
                return;
            }
        }
    }
}