using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldArray : OldValue
{
    private OldValue[]    Values { get; set; }
    private List<OldExpr> Va     { get; set; }

    public OldArray(List<OldExpr> valuesList)
    {
        Values = new OldValue[valuesList.Count];
        Va     = valuesList;
    }
    public OldArray(List<object> a) =>Values = a.Select(x => OldValue.ObjToValue(x)).ToArray();
    public override OldValue Run(ref VariateManager Manager)
    {
        for (int i = 0; i < Va.Count; i++)
            Values[i] = Va[i].Run(ref Manager);
        return this;
    }
    public OldValue Post(OldInt i,OldValue value)
    {
        if (i.Value < 0)
            i.Value = Values.Length+i.Value;
        var a = Values[i.Value];
        Values[i.Value] = value;
        return a;
    }
    public OldValue Get(OldInt a)
    {
        if (a.Value < 0)
            a.Value = Values.Length+a.Value;
        return Values[a.Value];
    }
    public override string ToString() => Values[0] == null?APIs.ListToString(Va):APIs.ArrayToString(Values);
    public override object GetValue() => APIs.ListToObjects(Values.ToList());
}