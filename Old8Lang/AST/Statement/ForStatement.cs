using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class ForStatement : OldStatement
{
    public SetStatement SetStatement { get; set; }

    public Operation Expr { get; set; }

    public OldStatement Statement { get; set; }

    public BlockStatement ForBlockStatement { get; set; }

    public ForStatement(SetStatement setStatement,Operation expr,OldStatement statement,BlockStatement blockStatement)
    {
        SetStatement      = setStatement;
        Expr              = expr;
        Statement         = statement;
        ForBlockStatement = blockStatement;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool expr = false;
        Manager.AddChildren();
        SetStatement.Run(ref Manager);
        while (true)
        {
            var varexpr = Expr.Run(ref Manager);
            if (varexpr is BoolValue)
                expr = (varexpr as BoolValue).Value;
            else
                return;


            if (expr)
            {
                ForBlockStatement.Run(ref Manager);
                Statement.Run(ref Manager);
            }
            else
            {
                Manager.RemoveChildren();
                return;
            }
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder($"for({SetStatement} ; {Expr} ; {Statement})");
        sb.Append("\n{"+ForBlockStatement+"\n}");
        return sb.ToString();
    }
}