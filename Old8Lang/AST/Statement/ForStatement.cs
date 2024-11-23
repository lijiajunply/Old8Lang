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
    public override void Run(ref VariateManager Manager)
    {
        Manager.AddChildren();
        setStatement.Run(ref Manager);
        while (true)
        {
            var varExpr = expr.Run(ref Manager);
            bool expr1;
            if (varExpr is BoolValue value)
                expr1 = value.Value;
            else
                break;
            if (expr1)
            {
                blockStatement.Run(ref Manager);
                statement.Run(ref Manager);
            }
            else
                break;
        }

        Manager.RemoveChildren();
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"for({setStatement} ; {expr} ; {statement})");
        sb.Append("\n{" + blockStatement + "\n}");
        return sb.ToString();
    }
}