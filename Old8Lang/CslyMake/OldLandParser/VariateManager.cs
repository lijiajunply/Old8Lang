using System.Text;
using Old8Lang.CslyMake.OldExpression;

namespace Old8Lang.CslyMake.OldLandParser;

public class VariateManager
{
    private int Count { get; set; } = 0;

    public List<int> VariateDirectValue { get; set; } = new List<int>();

    public List<int> ChildrenNum { get; set; } = new List<int>();

    public List<OldID> Variates { get; set; } = new List<OldID>();

    public Dictionary<int,OldValue> Values { get; set; } = new Dictionary<int,OldValue>();

    public bool IsReturn { get; set; } = false;

    public OldValue Result { get; set; } = new OldInt(0);

    public bool isClass { get; set; } = false;

    public List<OldValue> AnyInfo { get; set; } = new List<OldValue>();

    public ValueTuple<OldID,OldExpr> Set(OldID id,OldValue value)
    {
        var a1  = GetValue(id);
        if (a1 is null)
        {
            //init
            var a = value;
            Variates.Add(id);
            Values.Add(Count,a);
            VariateDirectValue.Add(Count);
            Count++;
            return (id,a);
        }
        else
        {
            //reset
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            Values[VariateDirectValue[a]] = value;
        }
        return (id,value);
    }

    public ValueTuple<OldID,OldValue> Direct(OldID id,OldID directID)
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
        return (id,Values[VariateDirectValue.Last()]);
    }

    public void GC()
    {
        if (Values.Count > Count)
        {
            for (int i = Values.Count; i > Count; i--)
            {
                Values.Remove(i-1);
            }
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
            Variates.RemoveAt(Count          -1);
            VariateDirectValue.RemoveAt(Count-1);
        }
        var a = ChildrenNum.Last();
        ChildrenNum.Remove(a);
        GC();
    }

    public OldValue GetValue(OldID id)
    {
        try
        {
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            if (a == -1)
            {
                var b = AnyInfo.FindLastIndex(x =>
                                                       {
                                                           switch (x)
                                                           {
                                                               case OldFunc func:
                                                               {
                                                                   var a = func;
                                                                   return a.ID.IdName == id.IdName;
                                                               }
                                                               case OldAny any:
                                                               {
                                                                   var a = any;
                                                                   return a.Id.IdName == id.IdName;
                                                               }
                                                               default:
                                                                   return false;
                                                           }
                                                       });
                var aa = AnyInfo[b];
                return aa.Clone();
            }
            else
                return Values[VariateDirectValue[a]];
        }
        catch (Exception e)
        {
            return null!;
        }
    }

    public void Init()
    {
        Values             = new Dictionary<int,OldValue>();
        Variates           = new List<OldID>();
        VariateDirectValue = new List<int>();
    }

    public void Init(Dictionary<string,OldValue> values)
    {
        foreach (var VARIABLE in values)
        {
            Variates.Add(new OldID(VARIABLE.Key));
            VariateDirectValue.Add(Count);
            Values.Add(Count,VARIABLE.Value);
            Count++;
        }
        isClass = true;
    }

    public void AddClassAndFunc(OldID id,OldValue value) => AnyInfo.Add(value);
    

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (var VARIABLE in Variates)
            builder.Append(VARIABLE+"|");
        builder.Append(Variates.Any()? "\n":"");
        foreach (var VARIABLE in VariateDirectValue)
            builder.Append(VARIABLE.ToString() != String.Empty? VARIABLE+"|":"");
        builder.Append(VariateDirectValue.Any()? "\n|":"");
        foreach (var VARIABLE in Values)
            builder.Append(VARIABLE.Value+"|");
        builder.Append(Values.Any()? "\n":"");
        foreach (var VARIABLE in AnyInfo)
            builder.Append(VARIABLE.Value+"|");
        return builder.ToString();
    }
}