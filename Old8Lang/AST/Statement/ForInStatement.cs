using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST.Statement;

public class ForInStatement(OldID id, OldExpr expr, OldStatement body) : OldStatement
{
    private OldID Id { get; } = id;
    private OldExpr Expr { get; } = expr;
    private OldStatement Body { get; } = body;

    public override void Run(ref VariateManager Manager)
    {
        Manager.AddChildren();

        var value = Expr.Run(ref Manager);
        if (value is not IOldList oldList) throw new Exception("ForInStatement: Expr is not IOldList");

        foreach (var idValue in oldList.GetItems())
        {
            Manager.Set(Id, idValue);
            Body.Run(ref Manager);
        }

        Manager.RemoveChildren();
    }
}