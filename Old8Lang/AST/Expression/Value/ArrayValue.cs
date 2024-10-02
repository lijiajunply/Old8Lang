using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class ArrayValue : ValueType , IOldList
{
    public ValueType[] Values { get; }
    private List<OldExpr> Va { get; } = [];

    public ArrayValue(IEnumerable<OldExpr> valuesList)
    {
        var oldExpr = valuesList as OldExpr[] ?? valuesList.ToArray();
        Values = new ValueType[oldExpr.Length];
        Va = oldExpr.ToList();
    }

    public ArrayValue(List<object> a) => Values = a.Select(ObjToValue).ToArray();

    public override ValueType Run(ref VariateManager Manager)
    {
        for (var i = 0; i < Va.Count; i++)
            Values[i] = Va[i].Run(ref Manager);
        return this;
    }

    public void Add(IntValue i, ValueType valueType)
    {
        if (i.Value < 0)
            i.Value = Values.Length + i.Value;
        Values[i.Value] = valueType;
    }

    public ValueType Get(IntValue a)
    {
        if (a.Value < 0)
            a.Value = Values.Length + a.Value;
        return Values[a.Value];
    }

    public override string ToString() => Values[0] == null! ? Apis.ListToString(Va) : Apis.ArrayToString(Values);
    public override object GetValue() => Apis.ListToObjects(Values.ToList());
    public IEnumerable<ValueType> GetItems() => Values;
    public int GetLength() => Values.Length;
}