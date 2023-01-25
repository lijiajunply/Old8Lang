using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldList : OldValue
{
    private new List<OldExpr> Value  { get; set; }

    private List<OldValue> Values { get; set; } = new List<OldValue>();

    public OldList(List<OldExpr> value) => Value = value;
    public OldList(List<object>  value) => Values = value.Select(x => OldValue.ObjToValue(x)).ToList();
    
    public override OldValue Run(ref VariateManager Manager)
    {
        foreach (var VARIABLE in Value)
            Values.Add(VARIABLE.Run(ref Manager));
        return this;
    }

    public OldValue Get(OldInt i)
    {
        if (i.Value < 0)
            i.Value = Values.Count+i.Value;
        return Values[i.Value];
    }

    private OldValue Add(OldValue value)
    {
        Values.Add(value);
        return value;
    }

    private OldValue Remove(OldInt num)
    {
        var a = Values[num.Value];
        Values.RemoveAt(num.Value);
        return a;
    }

    public override string ToString() =>
        !Values.Any() ? "{"+APIs.ListToString(Value)+"}" : "{"+APIs.ListToString(Values)+"}";

    public override OldValue Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldInstance a)
        {
            if (a.Id.IdName == "Add")
            {
               var result = a.Ids[0] as OldValue;
               return Add(result);
            }
            if (a.Id.IdName == "Remove")
            {
                var result = a.Ids[0] as OldInt;
                return Remove(result);
            }
        }
        return null;
    }

    public override object GetValue() => APIs.ListToObjects(Values);
}