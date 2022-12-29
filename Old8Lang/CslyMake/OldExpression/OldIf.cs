using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldIf : OldStatement
{
    public BinaryOperation Expr { get; set; }
    public OldBlock Block { get; set; }

    public OldIf(BinaryOperation expr, OldBlock block)
    {
        Expr = expr;
        Block = block;
    }

    public new void Run(ref VariateManager Manager,ref bool r)
    {
        if (r == false) return;
        var exprvalue = Expr.Run(ref Manager);
        if (exprvalue is OldBool)
        {
            bool vara = (bool)(exprvalue as OldBool).Value;
            if (vara)
            {
                Block.Run(ref Manager);
                r = false;
            }else return;
        }
        else return;
    }
}