using System.Text;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using String = System.String;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.CslyParser;

public class VariateManager
{
    #region Lang

    public LangInfo? LangInfo { get; set; }
    public string Path { get; set; } = String.Empty;

    #endregion

    #region Variate

    private Dictionary<string, ValueType> Variates { get; set; }

    public List<ValueType> AnyInfo { get; set; }

    #endregion

    #region Return

    public bool IsReturn { get; set; }
    public ValueType Result { get; set; }

    #endregion

    #region Block

    private int Count { get; set; }

    private List<int> ChildrenNum { get; set; }

    public bool IsClass { get; set; }

    #endregion

    public VariateManager()
    {
        ChildrenNum = new List<int>();
        Variates = new Dictionary<string, ValueType>();
        IsReturn = false;
        Result = new IntValue(0);
        IsClass = false;
        AnyInfo = new List<ValueType>();
    }

    public VariateManager(LangInfo info)
    {
        ChildrenNum = new List<int>();
        Variates = new Dictionary<string, ValueType>();
        IsReturn = false;
        Result = new IntValue(0);
        IsClass = false;
        AnyInfo = new List<ValueType>();
        LangInfo = info;
    }

    public ValueTuple<OldID, OldExpr> Set(OldID id, ValueType valueType)
    {
        var a1 = GetValue(id);
        if (a1 == null!)
        {
            //init
            var a = valueType;
            Variates.Add(id.IdName, valueType);
            Count++;
            return (id, a);
        }
        
        //reset
        Variates[id.IdName] = valueType;

        return (id, valueType);
    }


    /// <summary>
    /// GC
    /// </summary>
    private void GarbageCollection()
    {
    }

    public void AddChildren()
    {
        ChildrenNum.Add(int.Parse(Count.ToString()));
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
        if (Variates.Keys.Any(key => key == id.IdName))
            return Variates[id.IdName];

        var b = AnyInfo.Find(x =>
        {
            return x switch
            {
                FuncValue func => func.Id!.IdName == id.IdName,
                AnyValue any => any.Id.IdName == id.IdName,
                NativeAnyValue na => na.ClassName == id.IdName,
                NativeStaticAny staticAny => staticAny.ClassName == id.IdName,
                _ => false
            };
        });
        return b!;
    }

    #region Init

    public void Init()
    {
        Variates = new Dictionary<string, ValueType>();
    }

    public void Init(Dictionary<string, ValueType> values)
    {
        Variates = values;
        Count = values.Count;
    }

    #endregion


    public void AddClassAndFunc(ValueType valueType) => AnyInfo.Add(valueType);

    public VariateManager Clone() => (VariateManager)MemberwiseClone();

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (var variate in Variates)
            builder.Append(" " + variate.Key + "=>" + variate.Value + " ");
        builder.Append('\n');
        foreach (var variable in AnyInfo)
            builder.Append(variable + "\n");
        return builder.ToString();
    }

    public Dictionary<string, ValueType> Output() => Variates;
}