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

    //private Dictionary<string, ValueType> Variates { get; set; } = new();
    private List<string> VariateName { get; } = [];
    private List<ValueType> Values { get; } = [];

    public List<ValueType> AnyInfo { get; private init; } = [];

    #endregion

    #region Return

    public bool IsReturn { get; set; }
    public ValueType Result { get; set; } = new VoidValue();

    #endregion

    #region Block

    private int Count { get; set; }
    public bool IsFunc { get; set; }

    private List<int> ChildrenNum { get; } = [];

    public bool IsClass { get; set; }

    #endregion

    public void Set(OldID id, ValueType valueType)
    {
        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
            VariateName.Add(id.IdName);
            Values.Add(valueType);
            Count++;
            return;
        }

        //reset
        var count = VariateName.IndexOf(id.IdName);
        Values[count] = valueType;
    }

    public void AddChildren()
    {
        ChildrenNum.Add(Count);
    }

    public void RemoveChildren()
    {
        var num = ChildrenNum[^1];

        while (Count > num)
        {
            Values.RemoveAt(Count - 1);
            VariateName.RemoveAt(Count - 1);
            Count--;
        }

        ChildrenNum.Remove(ChildrenNum[^1]);
    }

    public ValueType? GetValue(OldID id)
    {
        var count = VariateName.IndexOf(id.IdName);
        if (count != -1) return Values[count];

        return GetAny(id);
    }

    public ValueType? GetAny(OldID id)
    {
        return AnyInfo.FirstOrDefault(x =>
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
    }

    #region Init

    public void Init(Dictionary<string, ValueType> values)
    {
        VariateName.AddRange(values.Keys);
        Values.AddRange(values.Values);
        Count = values.Count;
    }

    #endregion

    public VariateManager NewManger()
        => new() { AnyInfo = AnyInfo, LangInfo = LangInfo, Interpreter = Interpreter, Path = Path };

    public void AddClassAndFunc(ValueType valueType) => AnyInfo.Add(valueType);

    public VariateManager Clone() => (VariateManager)MemberwiseClone();

    public override string ToString()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < Values.Count; i++)
        {
            builder.AppendLine($"{VariateName[i]}:{Values[i]}");
        }

        return builder.ToString();
    }
}