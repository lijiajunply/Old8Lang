using Old8Lang.AST.Expression;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST.Statement;

public class SetListStatement(List<OldID> ids, List<OldExpr> expr) : OldStatement
{
    private List<OldID> Ids { get; } = ids;
    private List<OldExpr> Expr { get; } = expr;

    public override void Run(ref VariateManager Manager)
    {
        var results = new List<ValueType>();
        foreach (var item in Expr)
        {
            results.Add(item.Run(ref Manager));
        }

        for (int i = 0; i < results.Count; i++)
        {
            Manager.Set(Ids[i], results[i]);
        }
    }
}