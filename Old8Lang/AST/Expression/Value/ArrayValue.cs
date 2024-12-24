using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class ArrayValue : ValueType, IOldList
{
    private readonly ValueType[] RunResult;
    private readonly List<OldExpr> Values = [];

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

    public override ValueType Run(VariateManager Manager)
    {
        for (var i = 0; i < Values.Count; i++)
            RunResult[i] = Values[i].Run(Manager);
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

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 创建一个长度为 5 的整数数组
        var len = RunResult.Length;
        ilGenerator.Emit(OpCodes.Ldc_I4, len); // 加载数组长度
        ilGenerator.Emit(OpCodes.Newarr, typeof(object)); // 创建新数组

        for (var i = 0; i < len; i++)
        {
            ilGenerator.Emit(OpCodes.Dup); // 复制数组引用
            ilGenerator.Emit(OpCodes.Ldc_I4, i); // 加载索引 0
            Type t;
            if (len == Values.Count)
            {
                Values[i].LoadILValue(ilGenerator, local);
                t = Values[i].OutputType(local)!;
            }
            else
            {
                RunResult[i].LoadILValue(ilGenerator, local);
                t = RunResult[i].OutputType(local)!;
            }
            
            ilGenerator.Emit(OpCodes.Box, t); // 将 int 转换为 object

            ilGenerator.Emit(OpCodes.Stelem_Ref); // 将值存入数组
        }
    }

    public override Type OutputType(LocalManager local) => typeof(object[]);
}