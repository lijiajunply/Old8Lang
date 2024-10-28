using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class StringTreeList(List<OldExpr> list) : ValueType
{
    private List<OldExpr> List { get; set; } = list;
    
    public override ValueType Run(ref VariateManager Manager)
    {
        var result = "";
        foreach (var item in List)
        {
            var value = item.Run(ref Manager);
            result += value.ToString();
        }
        return new StringValue(result);
    }
}