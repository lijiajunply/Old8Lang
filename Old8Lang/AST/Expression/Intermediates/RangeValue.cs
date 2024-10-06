using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class RangeValue(OldExpr? start, OldExpr? end) : ValueType
{
    public override ValueType Run(ref VariateManager Manager)
    {
        var results = new List<ValueType>();

        var startValue = start?.Run(ref Manager);
        var endValue = end?.Run(ref Manager);

        if (startValue is not IntValue startIntValue || endValue is not IntValue endIntValue)
            throw new Exception("RangeValue: start or end is not IntValue");
        
        for (var i = startIntValue.Value; i <= endIntValue.Value; i++)
            results.Add(new IntValue(i));

        return new ArrayValue(results);
    }
}