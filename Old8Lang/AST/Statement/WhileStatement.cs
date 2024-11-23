using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public class WhileStatement(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    public override void Run(ref VariateManager Manager)
    {
        Manager.AddChildren();
        while (true)
        {
            var value = expr.Run(ref Manager);
            bool expr1;
            if (value is BoolValue varBool)
            {
                expr1 = varBool.Value;
            }
            else
            {
                throw new Exception($"Type Error: {value} is not Bool");
            }

            if (expr1)
            {
                blockStatement.Run(ref Manager);
            }
            else
            {
                Manager.RemoveChildren();
                return;
            }
        }
    }

    public override string ToString() => $"while({expr}){blockStatement}";
}