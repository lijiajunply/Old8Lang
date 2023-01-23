using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class OldIf : OldStatement
{
    public OldExpr Expr { get; set; }
    public BlockStatement BlockStatement { get; set; }

    public OldIf(OldExpr expr, BlockStatement blockStatement)
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

    public override string ToString() => $"{Expr} : {BlockStatement}";
}