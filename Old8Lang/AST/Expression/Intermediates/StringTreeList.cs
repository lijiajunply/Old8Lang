using Old8Lang.LangParser;
using Old8Lang.AST.Expression.Value;


namespace Old8Lang.AST.Expression;

public class StringTreeList(List<OldExpr> list) : ValueType
{
    public override ValueType Run(Old8Lang.LangParser.VariateManager Manager)
    {
        var result = list.Select(item => item.Run(Manager))
            .Aggregate("", (current, value) => current + value);

        return new StringValue(result);
    }
}