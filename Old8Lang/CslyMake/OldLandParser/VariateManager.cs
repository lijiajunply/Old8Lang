using Old8Lang.CslyMake.OldExpression;

namespace Old8Lang.CslyMake.OldLandParser;

public class VariateManager
{
    public int Count { get; set; } = 0;
    public List<int> VariateDirectValue { get; set; } = new List<int>();
    public List<int> ChildrenNum { get; set; } = new List<int>();
    public List<OldID> Variates { get; set; } = new List<OldID>();
    public Dictionary<int, OldValue> Values { get; set; } = new Dictionary<int, OldValue>();
    public bool IsReturn { get; set; } = false;
    public OldValue Result { get; set; }

    public bool isClass { get; set; } = false;

    public Dictionary<string, OldValue> ClassAndFuncInfo { get; set; } = new Dictionary<string, OldValue>();

    public ValueTuple<OldID, OldExpr> Set(OldID id, OldExpr value)
    {
        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
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
        }
        else
        {
            //reset
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            if (value is OldID)
            {
                var b = Variates.FindLastIndex(x => x.IdName == (value as OldID).IdName);
                Values[VariateDirectValue[a]] = Values[VariateDirectValue[b]];
            }
            else
            {
                Values[VariateDirectValue[a]] = value as OldValue;
            }
        }
        return (id, value);
    }

    public ValueTuple<OldID, OldValue> Direct(OldID id, OldID directID)
    {

        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
            var a = Variates.FindIndex(x => x.IdName == directID.IdName);
            Variates.Add(id);
            VariateDirectValue.Add(VariateDirectValue[a]);
            Count++;
        }
        else
        {
            //re_direct
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            var b = Variates.FindLastIndex(x => x.IdName == directID.IdName);
            VariateDirectValue[a] = VariateDirectValue[b];
        }
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
        ChildrenNum.Add(Count+1);
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
        //GC();
    }

    public OldValue GetValue(OldID id)
    {
        try
        {
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            if (a == -1)
            {
                var func = ClassAndFuncInfo[id.IdName];
                if (func is null)
                    return null;
                return func;
            }
            else
            {
                int b = VariateDirectValue[a];
                return Values[b];
            }
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public void Init()
    {
        Values = new Dictionary<int, OldValue>();
        Variates = new List<OldID>();
        VariateDirectValue = new List<int>();
    }

    public void Init(Dictionary<OldID, OldValue> values)
    {
        int i = 0;
        foreach (var VARIABLE in values)
        {
            Variates.Add(VARIABLE.Key);
            VariateDirectValue.Add(i);
            Values.Add(i,VARIABLE.Value);
            i++;
        }
        isClass = true;
    }

    public ValueTuple<OldID, OldValue> AddClassAndFunc(OldID id, OldValue value)
    {
        ClassAndFuncInfo.Add(id.IdName,value);
        return (id, value);
    }
}