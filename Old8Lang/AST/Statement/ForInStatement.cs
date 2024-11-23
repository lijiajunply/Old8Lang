using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ForInStatement(OldID id, OldExpr expr, OldStatement body) : OldStatement
{
    public override void Run(ref VariateManager Manager)
    {
        Manager.AddChildren();

        var value = expr.Run(ref Manager);
        if (value is not IOldList oldList)
            throw new Exception("ForInStatement: Expr is not IOldList");

        foreach (var idValue in oldList.GetItems())
        {
            Manager.Set(id, idValue);
            body.Run(ref Manager);
        }

        Manager.RemoveChildren();
    }
}