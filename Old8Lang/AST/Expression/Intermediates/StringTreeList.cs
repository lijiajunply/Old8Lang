using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class StringTreeList(List<OldExpr> list) : ValueType
{
    public override ValueType Run(VariateManager Manager)
    {
        var result = list.Select(item => item.Run(Manager))
            .Aggregate("", (current, value) => current + value);

        return new StringValue(result);
    }
}