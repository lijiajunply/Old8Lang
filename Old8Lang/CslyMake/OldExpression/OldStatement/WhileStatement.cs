using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// while语句
/// </summary>
public class WhileStatement : OldStatement
{
    public OldExpr Expr { get; set; }
    public BlockStatement BlockStatement { get; set; }

    public WhileStatement(OldExpr expr, BlockStatement blockStatement)
    {
        Expr = expr;
        BlockStatement = blockStatement;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool expr = false;
        Manager.AddChildren();
        while (true)
        {
            var varbool = Expr.Run(ref Manager);
            if (varbool is OldBool)
            {
                expr = (bool)(Expr.Run(ref Manager) as OldBool).Value;
            }
            else
            {
                return;
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
    public override string ToString() => $"{Expr} : {BlockStatement}";
}