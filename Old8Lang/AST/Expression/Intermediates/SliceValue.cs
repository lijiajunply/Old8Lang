using System.Collections;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class SliceValue(OldID id, OldExpr? start = null, OldExpr? end = null) : ValueType
{
    private OldID Id { get; } = id;
    private OldExpr? Start { get; } = start;
    private OldExpr? End { get; } = end;

    public override ValueType Run(ref VariateManager Manager)
    {
        var value = Id.Run(ref Manager);
        var start = Start?.Run(ref Manager);
        var end = End?.Run(ref Manager);

        var length = value.GetValue<IList>().Count;
        var startValue = start?.GetValue<int>() ?? 0;
        var endValue = end?.GetValue<int>() ?? length;

        if (startValue < 0) startValue += length;
        if (endValue < 0) endValue += length + 1;

        return value switch
        {
            StringValue str => new StringValue(str.Value[startValue..endValue]),
            ArrayValue arr => new ArrayValue(arr.Values[startValue..endValue]),
            ListValue list => new ListValue(list.Values[startValue..endValue]
                .OfType<OldExpr>()
                .ToList()),
            _ => new VoidValue()
        };
    }
}