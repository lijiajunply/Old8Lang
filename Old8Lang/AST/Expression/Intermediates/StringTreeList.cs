using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.Intermediates;

public class StringTreeList(List<OldExpr> list) : ValueType
{
    public override ValueType Run(LangParser.VariateManager manager)
    {
        var result = list.Select(item => item.Run(manager))
            .Aggregate("", (current, value) => current + value);

        return new StringValue(result);
    }
}