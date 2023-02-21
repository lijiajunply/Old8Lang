using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class OldIf : OldStatement
{
    private OldExpr        Expr           { get; set; }

    private BlockStatement BlockStatement { get; set; }

    public OldIf(OldExpr expr, BlockStatement blockStatement)
    {
        Expr = expr;
        BlockStatement = blockStatement;
    }

    public void Run(ref VariateManager Manager,ref bool r)
    {
        if (r == false) return;
        var exprvalue = Expr.Run(ref Manager);
        if (exprvalue is BoolValue value)
        {
            if (value.Value)
            {
                BlockStatement.Run(ref Manager);
                r = false;
            }
        }
    }

    public override string ToString() => $"({Expr})\n{BlockStatement}";
}