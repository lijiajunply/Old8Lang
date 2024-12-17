using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class SliceValue(OldID id, OldExpr? start = null, OldExpr? end = null) : ValueType
{
    public override ValueType Run(VariateManager Manager)
    {
        var value = id.Run(Manager);
        var start1 = start?.Run(Manager);
        var end1 = end?.Run(Manager);

        if (value is not IOldList list) return new VoidValue();

        var length = list.GetLength();
        var startValue = start1?.GetValue<int>() ?? 0;
        var endValue = end1?.GetValue<int>() ?? length;

        return list.Slice(startValue, endValue);
    }
}