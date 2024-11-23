using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class OldIf(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    public void Run(ref VariateManager Manager, ref bool r)
    {
        if (r == false) return;
        var exprValue = expr.Run(ref Manager);
        if (exprValue is not BoolValue { Value: true }) return;
        blockStatement.Run(ref Manager);
        r = false;
    }

    public override string ToString() => $"({expr})\n{blockStatement}";
}