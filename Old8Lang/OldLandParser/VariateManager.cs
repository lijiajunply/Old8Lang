using System.Text;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using String = System.String;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.OldLandParser;

public class VariateManager
{
    public LangInfo LangInfo { get; set; }

    public string Path { get; set; } = String.Empty;

    private int Count { get; set; }


    private List<int> ChildrenNum { get; set; }

    private Dictionary<string,ValueType> Variates { get; set; }

    public bool IsReturn { get; set; }

    public ValueType Result { get; set; }

    public bool IsClass { get; set; }

    public List<ValueType> AnyInfo { get; set; }

    public VariateManager()
    {
        ChildrenNum = new List<int>();
        Variates    = new Dictionary<string,ValueType>();
        IsReturn    = false;
        Result      = new IntValue(0);
        IsClass     = false;
        AnyInfo     = new List<ValueType>();
    }

    public ValueTuple<OldID,OldExpr> Set(OldID id,ValueType valueType)
    {
        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
            var a = valueType;
            Variates.Add(id.IdName,valueType);
            Count++;
            return (id,a);
        }
        else
        {
            //reset
            Variates[id.IdName] = valueType;
        }
        return (id,valueType);
    }


    private void GarbageCollection()
    {

    }

    public void AddChildren()
    {
        ChildrenNum.Add(Int32.Parse(Count.ToString()));
    }

    public void RemoveChildren()
    {
        var num = ChildrenNum[^1];
        
        while (Count > num)
        {
            var a = Variates.Keys.ToList();
            Count--;
            Variates.Remove(a[Count]);
        }
        ChildrenNum.Remove(ChildrenNum[^1]);
        GarbageCollection();
    }

    public ValueType GetValue(OldID id)
    {
        foreach (var key in Variates.Keys)
            if (key == id.IdName)
                return Variates[id.IdName];

        var b = AnyInfo.Find(x =>
                             {
                                 switch (x)
                                 {
                                     case FuncValue func:
                                         return func.Id.IdName == id.IdName;
                                     case AnyValue any:
                                         return any.Id.IdName == id.IdName;
                                     case NativeAnyValue na:
                                         return na.ClassName == id.IdName;
                                     case NativeStaticAny staticAny:
                                         return staticAny.ClassName == id.IdName;
                                     default:
                                         return false;
                                 }
                             });
        return b;
    }

    public void Init()
    {
        Variates = new Dictionary<string,ValueType>();
    }

    public void Init(Dictionary<string,ValueType> values)
    {
        Variates = values;
        Count    = values.Count;
    }

    public void AddClassAndFunc(ValueType valueType) => AnyInfo.Add(valueType);

    public VariateManager Clone() => (VariateManager)MemberwiseClone();

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (var variate in Variates)
            builder.Append(" "+variate.Key+"=>"+variate.Value+" ");
        builder.Append("\n");
        foreach (var variable in AnyInfo)
            builder.Append(variable+"\n");
        return builder.ToString();
    }

    public Dictionary<string,ValueType> Output() => Variates;
}