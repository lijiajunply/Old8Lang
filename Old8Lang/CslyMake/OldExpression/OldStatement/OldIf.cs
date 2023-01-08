using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldIf : OldStatement
{
    public BinaryOperation Expr { get; set; }
    public BlockStatement BlockStatement { get; set; }

    public OldIf(BinaryOperation expr, BlockStatement blockStatement)
    {
        Expr = expr;
        BlockStatement = blockStatement;
    }

    public new void Run(ref VariateManager Manager,ref bool r)
    {
        if (r == false) return;
        var exprvalue = Expr.Run(ref Manager);
        if (exprvalue is OldBool)
        {
            var a = exprvalue as OldBool;
            if ((bool)a.Value)
            {
                BlockStatement.Run(ref Manager);
                r = false;
            }else return;
        }
        else return;
    }
}