using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class OldIf(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    private OldExpr Expr { get; } = expr;

    private BlockStatement BlockStatement { get; } = blockStatement;

    public void Run(ref VariateManager Manager, ref bool r)
    {
        if (r == false) return;
        var exprValue = Expr.Run(ref Manager);
        if (exprValue is BoolValue value)
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