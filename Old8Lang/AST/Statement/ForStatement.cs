using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ForStatement(
    SetStatement setStatement,
    Operation expr,
    OldStatement statement,
    BlockStatement blockStatement)
    : OldStatement
{
    private SetStatement SetStatement { get; set; } = setStatement;

    private Operation Expr { get; set; } = expr;

    private OldStatement Statement { get; set; } = statement;

    private BlockStatement ForBlockStatement { get; set; } = blockStatement;

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddChildren();
        SetStatement.Run(ref Manager);
        while (true)
        {
            var varExpr = Expr.Run(ref Manager);
            bool expr;
            if (varExpr is BoolValue value)
                expr = value.Value;
            else
                break;
            if (expr)
            {
                ForBlockStatement.Run(ref Manager);
                Statement.Run(ref Manager);
            }
            else
                break;
        }

        Manager.RemoveChildren();
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"for({SetStatement} ; {Expr} ; {Statement})");
        sb.Append("\n{" + ForBlockStatement + "\n}");
        return sb.ToString();
    }
}