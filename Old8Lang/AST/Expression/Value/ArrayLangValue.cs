using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 数列
/// </summary>
public class ArrayLangValue : LangValueType, ILangList
{
    private readonly LangValueType[] RunResult;
    private readonly List<OldExpr> Values = [];

    public ArrayLangValue(IEnumerable<OldExpr> valuesList, SourcePosition position = default) : base(position)
    {
        var oldExpr = valuesList as OldExpr[] ?? [.. valuesList];
        RunResult = new LangValueType[oldExpr.Length];
        Values = [.. oldExpr];
    }

    public ArrayLangValue(List<LangValueType> re, SourcePosition position = default) : base(position)
    {
        RunResult = [.. re];
        Values = new List<OldExpr>(); // 初始化空列表，因为我们已经有了RunResult
    }

    public ArrayLangValue(List<object> a, SourcePosition position = default) : base(position) =>
        RunResult = [.. a.Select(ObjToValue)];

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        for (var i = 0; i < Values.Count; i++)
            RunResult[i] = Values[i].Run(manager);
        return this;
    }

    public void Set(LangValueType index, LangValueType value)
    {
        if (index is IntLangValue i)
        {
            if (i.Value >= RunResult.Length || i.Value < -RunResult.Length)
                throw new IndexError(this, i.Value, RunResult.Length);
            if (i.Value < 0)
                i.Value = RunResult.Length + i.Value;
            RunResult[i.Value] = value;
        }
        else
        {
            throw new TypeError(this, "IntValue", index.GetType().Name);
        }
    }

    public LangValueType Get(IntLangValue a)
    {
        var index = a.Value;
        if (index < 0)
            index = RunResult.Length + index;
        if (index < 0 || index >= RunResult.Length)
            throw new IndexError(this, index, RunResult.Length);
        return RunResult[index];
    }

    public override string ToString() =>
        RunResult.Length == 0 ? "[]" :
        RunResult.Length > 0 && RunResult[0] == null! ? $"[{string.Join(", ", Values)}]" :
        $"[{string.Join(", ", RunResult)}]"; // Old8Lang 风格的数组，使用 [ ] 包裹

    public override object GetValue() => Apis.ListToObjects(RunResult.ToList());
    public IEnumerable<LangValueType> GetItems() => RunResult;
    public int GetLength() => RunResult.Length;

    public LangValueType Slice(int start, int end)
    {
        if (start < 0) start += RunResult.Length;
        if (end < 0) end += RunResult.Length + 1;
        return new ArrayLangValue(RunResult[start..end]);
    }

    public Type GetChildType() => typeof(object);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
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
                Values[i].LoadIlValue(ilGenerator, local);
                t = Values[i].OutputType(local)!;
            }
            else
            {
                RunResult[i].LoadIlValue(ilGenerator, local);
                t = RunResult[i].OutputType(local)!;
            }

            ilGenerator.Emit(OpCodes.Box, t); // 将 int 转换为 object

            ilGenerator.Emit(OpCodes.Stelem_Ref); // 将值存入数组
        }
    }

    public override Type OutputType(LocalManager local) => typeof(object[]);
    
    public override LangValueType Converse(LangValueType otherLangValueType, LangParser.VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value) 
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        switch (value.Value)
        {
            case "List" or "list":
                // 数组转换为列表
                return new ListLangValue(RunResult.ToList(), Position);
            case "Array" or "array":
                // 已经是数组，直接返回
                return this;
            case "String" or "string":
                // 数组转换为字符串，用逗号分隔
                return new StringLangValue(string.Join(", ", RunResult.Select(v => v.ToDisplayString())));
            default:
                throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}");
        }
    }
}