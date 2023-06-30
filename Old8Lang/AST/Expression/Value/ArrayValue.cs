using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class ArrayValue : ValueType
{
    private ValueType[] Values { get; set; }
    private List<OldExpr> Va { get; set; }

    public ArrayValue(List<OldExpr> valuesList)
    {
        Values = new ValueType[valuesList.Count];
        Va = valuesList;
    }

    public ArrayValue(List<object> a) => Values = a.Select(x => ValueType.ObjToValue(x)).ToArray();

    public override ValueType Run(ref VariateManager Manager)
    {
        for (int i = 0; i < Va.Count; i++)
            Values[i] = Va[i].Run(ref Manager);
        return this;
    }

    public ValueType Post(IntValue i, ValueType valueType)
    {
        if (i.Value < 0)
            i.Value = Values.Length + i.Value;
        var a = Values[i.Value];
        Values[i.Value] = valueType;
        return a;
    }

    public ValueType Get(IntValue a)
    {
        if (a.Value < 0)
            a.Value = Values.Length + a.Value;
        return Values[a.Value];
    }

    public override string ToString() => Values[0] == null ? Apis.ListToString(Va) : Apis.ArrayToString(Values);
    public override object GetValue() => Apis.ListToObjects(Values.ToList());
}