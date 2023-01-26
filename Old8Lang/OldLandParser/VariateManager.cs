using System.Text;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using String = System.String;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.OldLandParser;

public class VariateManager
{
    public  string Path  { get; set; } = String.Empty;
    private int    Count { get; set; }

    private List<int> VariateDirectValue { get; set; } = new List<int>();

    private List<int> ChildrenNum { get; set; } = new List<int>();

    private List<OldID> Variates { get; set; } = new List<OldID>();

    private Dictionary<int,ValueType> Values { get; set; } = new Dictionary<int,ValueType>();

    public bool IsReturn { get; set; }

    public ValueType Result { get; set; } = new IntValue(0);

    public bool IsClass { get; set; }

    public List<ValueType> AnyInfo { get; set; } = new List<ValueType>();

    public ValueTuple<OldID,OldExpr> Set(OldID id,ValueType valueType)
    {
        var a1  = GetValue(id);
        if (a1 is null)
        {
            //init
            var a = valueType;
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
            Values[VariateDirectValue[a]] = valueType;
        }
        return (id,valueType);
    }

    public ValueTuple<OldID,ValueType> Direct(OldID id,OldID directId)
    {

        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
            var a = Variates.FindIndex(x => x.IdName == directId.IdName);
            Variates.Add(id);
            VariateDirectValue.Add(VariateDirectValue[a]);
            Count++;
        }
        else
        {
            //re_direct
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            var b = Variates.FindLastIndex(x => x.IdName == directId.IdName);
            VariateDirectValue[a] = VariateDirectValue[b];
        }
        return (id,Values[VariateDirectValue.Last()]);
    }

    private void GarbageCollection()
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
        GarbageCollection();
    }

    public ValueType GetValue(OldID id)
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
                                                               case FuncValue func:
                                                                   return func.Id.IdName == id.IdName;
                                                               case AnyValue any:
                                                                   return any.Id.IdName == id.IdName;
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
        catch (Exception _)
        {
            return null!;
        }
    }

    public void Init()
    {
        Values             = new Dictionary<int,ValueType>();
        Variates           = new List<OldID>();
        VariateDirectValue = new List<int>();
    }

    public void Init(Dictionary<string,ValueType> values)
    {
        foreach (var variable in values)
        {
            Variates.Add(new OldID(variable.Key));
            VariateDirectValue.Add(Count);
            Values.Add(Count,variable.Value);
            Count++;
        }
        IsClass = true;
    }

    public void AddClassAndFunc(OldID id,ValueType valueType) => AnyInfo.Add(valueType);
    

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (var variable in Variates)
            builder.Append((string?)(variable+"|"));
        builder.Append(Variates.Any()? "\n":"");
        foreach (var variable in VariateDirectValue)
            builder.Append(variable.ToString() != String.Empty? variable+"|":"");
        builder.Append(VariateDirectValue.Any()? "\n|":"");
        foreach (var variable in Values)
            builder.Append((string?)(variable.Value+"|"));
        builder.Append(Values.Any()? "\n":"");
        foreach (var variable in AnyInfo)
            builder.Append(variable+"|");
        return builder.ToString();
    }
}