using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.CslyParser;

public class VariateManager
{
    #region Lang

    public LangInfo? LangInfo { get; set; }
    public string Path { get; set; } = "";
    
    public Interpreter? Interpreter { get; set; }

    #endregion

    #region Variate

    private Dictionary<string, ValueType> Variates { get; set; } = new();

    public List<ValueType> AnyInfo { get; set; } = [];

    #endregion

    #region Return

    public bool IsReturn { get; set; }
    public ValueType Result { get; set; } = new IntValue(0);

    #endregion

    #region Block

    private int Count { get; set; }

    private List<int> ChildrenNum { get; set; } = [];

    public bool IsClass { get; set; }

    #endregion

    public void Set(OldID id, ValueType valueType)
    {
        var a1 = GetValue(id);
        if (a1 == null!)
        {
            //init
            Variates.Add(id.IdName, valueType);
            Count++;
            return;
        }
        
        //reset
        Variates[id.IdName] = valueType;
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
        var builder = new StringBuilder();
        foreach (var variate in Variates)
            builder.Append(" " + variate.Key + "=>" + variate.Value + " ");
        builder.Append('\n');
        foreach (var variable in AnyInfo)
            builder.Append(variable + "\n");
        return builder.ToString();
    }
}