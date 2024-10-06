using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class ArrayValue : ValueType, IOldList
{
    private ValueType[] RunResult { get; }
    private List<OldExpr> Values { get; } = [];

    public ArrayValue(IEnumerable<OldExpr> valuesList)
    {
        var oldExpr = valuesList as OldExpr[] ?? valuesList.ToArray();
        RunResult = new ValueType[oldExpr.Length];
        Values = oldExpr.ToList();
    }

    public ArrayValue(List<ValueType> re)
    {
        RunResult = re.ToArray();
    }

    public ArrayValue(List<object> a) => RunResult = a.Select(ObjToValue).ToArray();

    public override ValueType Run(ref VariateManager Manager)
    {
        for (var i = 0; i < Values.Count; i++)
            RunResult[i] = Values[i].Run(ref Manager);
        return this;
    }

    public void Set(IntValue i, ValueType valueType)
    {
        if (i.Value >= RunResult.Length) throw new ErrorException(this, i);
        if (i.Value < 0)
            i.Value = RunResult.Length + i.Value;
        RunResult[i.Value] = valueType;
    }

    public ValueType Get(IntValue a)
    {
        if (a.Value < 0)
            a.Value = RunResult.Length + a.Value;
        return RunResult[a.Value];
    }

    public override string ToString() =>
        RunResult[0] == null! ? Apis.ListToString(Values) : Apis.ArrayToString(RunResult);

    public override object GetValue() => Apis.ListToObjects(RunResult.ToList());
    public IEnumerable<ValueType> GetItems() => RunResult;
    public int GetLength() => RunResult.Length;

    public ValueType Slice(int start, int end)
    {
        if (start < 0) start += RunResult.Length;
        if (end < 0) end += RunResult.Length + 1;
        return new ArrayValue(RunResult[start..end]);
    }
}