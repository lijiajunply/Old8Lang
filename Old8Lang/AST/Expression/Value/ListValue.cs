using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class ListValue : ValueType, IOldList
{
    private List<OldExpr> Value { get; } = [];

    public List<ValueType> Values { get; } = [];

    public ListValue(List<OldExpr> value) => Value = value;
    public ListValue(List<object> value) => Values = value.Select(ObjToValue).ToList();

    public override ValueType Run(ref VariateManager Manager)
    {
        foreach (var expr in Value)
            Values.Add(expr.Run(ref Manager));
        return this;
    }

    public ValueType Get(IntValue i)
    {
        if (i.Value < 0)
            i.Value = Values.Count + i.Value;
        return Values[i.Value];
    }

    public override string ToString() =>
        "{" + Apis.ListToString(Values) + "}";

    public override ValueType Dot(OldExpr dotExpr)
    {
        return dotExpr is not Instance a ? new VoidValue() : a.FromClassToResult(this);
    }

    public override object GetValue() => Apis.ListToObjects(Values);
    public IEnumerable<ValueType> GetItems() => Values;

    public int GetLength() => Values.Count;

    public ValueType Slice(int start, int end)
    {
        if (start < 0) start += Values.Count;
        if (end < 0) end += Values.Count + 1;
        return new ListValue(Values[start..end]
            .OfType<OldExpr>()
            .ToList());
    }
}