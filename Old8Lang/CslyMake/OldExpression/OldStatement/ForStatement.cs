using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class ForStatement : OldStatement
{
    public SetStatement SetStatement { get; set;}
    public BinaryOperation Expr { get; set; }
    public OldStatement Statement { get; set; }
    public BlockStatement ForBlockStatement { get; set; }

    public ForStatement(SetStatement setStatement, BinaryOperation expr, OldStatement statement, BlockStatement blockStatement)
    {
        SetStatement = setStatement;
        Expr = expr;
        Statement = statement;
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
            if (varexpr is OldBool)
            {
                expr = (varexpr as OldBool).Value;
            }
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
}