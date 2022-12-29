using Old8Lang.CslyMake.OldExpression;

namespace Old8Lang.CslyMake.OldLandParser;

public class VariateManager
{
    public int Count { get; set; } = 0;
    public List<int> VariateDirectValue { get; set; } = new List<int>();
    public List<int> ChildrenNum { get; set; } = new List<int>();
    public List<OldID> Variates { get; set; } = new List<OldID>();
    public Dictionary<int, OldValue> Values { get; set; } = new Dictionary<int, OldValue>();

    public ValueTuple<OldID, OldExpr> Set(OldID id, OldExpr value)
    {
        if (value is OldValue)
        {
            var a =  value as OldValue;
            Variates.Add(id);
            Values.Add(Count,a);
            VariateDirectValue.Add(Count);
            Count++;
            return (id,a);
        }else if (value is OldID)
        {
            var a = value as OldID;
            return Direct(id, a);
        }

        return (id, value);
    }

    public ValueTuple<OldID, OldValue> Direct(OldID id, OldID directID)
    {
        var a = Variates.FindIndex(x => x == directID);
        Variates.Add(id);
        VariateDirectValue.Add(VariateDirectValue[a]);
        Count++;
        return (id, Values[VariateDirectValue.Last()]);
    }

    public void GC()
    {
        var a = from variateDirectValue in VariateDirectValue
            from value in Values
            where value.Key != variateDirectValue
            select value.Key;
        foreach (var VARIABLE in a)
        {
            Values.Remove(VARIABLE);
        }
    }

    public void AddChildren()
    {
        ChildrenNum.Add(Count);
    }

    public void RemoveChildren()
    {
        for (; Count >= ChildrenNum.Last(); Count--)
        {
            Variates.RemoveAt(Count - 1);
            VariateDirectValue.RemoveAt(Count - 1);
        }
        var a = ChildrenNum.Last();
        ChildrenNum.Remove(a);
        GC();
    }

    public OldValue GetValue(OldID id)
    {
        var a = Variates.FindLastIndex(x => x == id);
        int b = VariateDirectValue[a];
        return Values[b];
    }
}