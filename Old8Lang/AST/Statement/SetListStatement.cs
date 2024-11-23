using Old8Lang.AST.Expression;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST.Statement;

public class SetListStatement(List<OldID> ids, List<OldExpr> expr) : OldStatement
{
    public override void Run(ref VariateManager Manager)
    {
        var results = new List<ValueType>();
        foreach (var item in expr)
        {
            results.Add(item.Run(ref Manager));
        }

        for (int i = 0; i < results.Count; i++)
        {
            Manager.Set(ids[i], results[i]);
        }
    }
}