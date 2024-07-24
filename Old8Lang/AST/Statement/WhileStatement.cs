using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public class WhileStatement(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    private OldExpr Expr { get; } = expr;
    private BlockStatement BlockStatement { get; } = blockStatement;

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddChildren();
        while (true)
        {
            var value = Expr.Run(ref Manager);
            bool expr;
            if (value is BoolValue varBool)
            {
                expr = varBool.Value;
            }
            else
            {
                throw new Exception($"Type Error: {value} is not Bool");
            }

            if (expr)
            {
                BlockStatement.Run(ref Manager);
            }
            else
            {
                Manager.RemoveChildren();
                return;
            }
        }
    }

    public override string ToString() => $"while({Expr}){BlockStatement}";
}