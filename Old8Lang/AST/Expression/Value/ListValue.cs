using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class ListValue : ValueType
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
        Values.Count == 0 ? "{" + Apis.ListToString(Value) + "}" : "{" + Apis.ListToString(Values) + "}";

    public override ValueType Dot(OldExpr dotExpr)
    {
        return dotExpr is not Instance a ? new VoidValue() : a.FromClassToResult(this,GetType().ToString());
    }

    public override object GetValue() => Apis.ListToObjects(Values);
}

public static class ListValueFuncStatic
{
    public static ValueType Add(this ListValue value,ValueType valueType)
    {
        value.Values.Add(valueType);
        return valueType;
    }

    private static ValueType Remove(this ListValue value,IntValue num)
    {
        var a = value.Values[num.Value];
        value.Values.RemoveAt(num.Value);
        return a;
    }

    private static VoidValue AddList(this ListValue value, ListValue otherValue)
    {
        value.Values.AddRange(otherValue.Values);
        return new VoidValue();
    }

    private static ListValue Sort(this ListValue value)
    {
        value.Values.Sort();
        return value;
    }
}