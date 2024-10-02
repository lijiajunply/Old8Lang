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

        if (value is not IOldList list) return new VoidValue();

        var length = list.GetLength();
        var startValue = start?.GetValue<int>() ?? 0;
        var endValue = end?.GetValue<int>() ?? length;

        return list.Slice(startValue, endValue);
    }
}