using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class StringTreeList(List<OldExpr> list) : ValueType
{
    public override ValueType Run(ref VariateManager Manager)
    {
        var result = "";
        foreach (var item in list)
        {
            var value = item.Run(ref Manager);
            result += value.ToString();
        }
        return new StringValue(result);
    }
}