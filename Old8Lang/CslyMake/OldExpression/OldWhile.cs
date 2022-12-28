using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// while语句
/// </summary>
public class OldWhile : OldCompound
{
    public OldExpr Expr { get; set; }
    public OldBlock Block { get; set; }

    public OldWhile(OldExpr expr, OldBlock block)
    {
        Expr = expr;
        Block = block;
    }

    public override void Run(ref VariateManager Manager)
    {
        while (true)
        {
            if (Expr.CompareRun(ref Manager))
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