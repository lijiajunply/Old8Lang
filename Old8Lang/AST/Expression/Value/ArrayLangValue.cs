using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 数列
/// </summary>
public class ArrayLangValue : LangValueType, ILangList
{
    private readonly LangValueType[] RunResult; // 保持固定大小数组
    private readonly List<LangExpression> Values = [];

    public ArrayLangValue(IEnumerable<LangExpression> valuesList, SourcePosition position = default) : base(position)
    {
        var oldExpr = valuesList as LangExpression[] ?? [.. valuesList];
        RunResult = new LangValueType[oldExpr.Length];
        Values.AddRange(oldExpr);
    }

    public ArrayLangValue(List<LangValueType> re, SourcePosition position = default) : base(position)
    {
        RunResult = [.. re];
        Values = []; // 初始化空列表，因为我们已经有了RunResult
    }

    public ArrayLangValue(List<object> a, SourcePosition position = default) : base(position) =>
        RunResult = [.. a.Select(ObjToValue)];

    public override LangValueType Run(VariateManager manager)
    {
        for (var i = 0; i < Values.Count; i++)
            RunResult[i] = Values[i].Run(manager);
        return this;
    }

    public void Set(LangValueType index, LangValueType value)
    {
        if (index is IntLangValue i)
        {
            int idx = i.Value;
            if (idx >= RunResult.Length || idx < -RunResult.Length)
                throw new IndexError(this, idx, RunResult.Length);
            if (idx < 0)
                idx = RunResult.Length + idx;

            // 支持动态类型数组：允许数组中包含不同类型的元素
            // 直接使用新值，不进行类型转换
            LangValueType convertedValue = value;

            RunResult[idx] = convertedValue;
        }
        else
        {
            throw new TypeError(this, "IntValue", index.GetType().Name);
        }
    }

    public bool In(LangValueType value)
    {
        return RunResult.Any(t => t.Equal(value));
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

    // 覆盖 Dot 方法以支持嵌套索引访问和方法调用，如 array[0][0] 和 array.Sort()
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 如果是Instance，可能是方法调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            // 检查是否是已知的 Array 方法，如果找到就调用 FromClassToResult
            var knownMethods = new[] { "Count", "Sort", "Distinct", "Map", "Filter", "Reduce", "Get", "Set", "Length", "ToList" };
            if (knownMethods.Contains(methodName))
            {
                return instance.FromClassToResult(this);
            }
        }

        // 如果 dotExpression 是一个整数值或可以转换为整数的表达式，则视为索引访问
        if (dotExpression is IntLangValue intValue)
        {
            return Get(intValue);
        }

        // 如果是其他类型的表达式，尝试将其作为索引（可能需要运行表达式）
        var result = dotExpression.Run(manager);

        if (result is IntLangValue idx)
        {
            return Get(idx);
        }

        // 如果不是索引访问，调用父类的 Dot 方法（会报错）
        return base.Dot(dotExpression, manager);
    }

    // 覆盖 Equal 方法以支持数组深度比较
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is not ArrayLangValue otherArray)
            return false;

        // 比较长度
        if (RunResult.Length != otherArray.RunResult.Length)
            return false;

        // 逐个比较元素
        for (int i = 0; i < RunResult.Length; i++)
        {
            if (!RunResult[i].Equal(otherArray.RunResult[i]))
                return false;
        }

        return true;
    }

    public override string ToString() =>
        RunResult.Length == 0 ? "[]" :
        RunResult.Length > 0 && RunResult[0] == null! ? $"[{string.Join(", ", Values)}]" :
        $"[{string.Join(", ", RunResult)}]"; // Old8Lang 风格的数组，使用 [ ] 包裹

    public override object GetValue() => Apis.ListToObjects(RunResult.ToList());
    public IEnumerable<LangValueType> GetItems() => RunResult;
    public int GetLength() => RunResult.Length;

    public LangValueType Slice(int start, int end, int step)
    {
        var length = RunResult.Length;
        var result = new List<LangValueType>();

        if (step > 0)
        {
            // 正向切片
            if (start < 0) start += length;
            if (end < 0) end += length;

            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            for (int i = start; i < end; i += step)
            {
                result.Add(RunResult[i]);
            }
        }
        else if (step < 0)
        {
            // 反向切片
            // 处理负数索引，但保留 -1 作为特殊值（表示"到开头之前"）
            if (start < -1) start += length;
            if (end < -1) end += length;

            // 设置边界
            if (start >= length) start = length - 1;
            if (start < -1) start = -1;
            if (end >= length) end = length - 1;

            for (int i = start; i > end; i += step)
            {
                result.Add(RunResult[i]);
            }
        }
        else
        {
            throw new InvalidOperationError(this, "切片步长不能为0");
        }

        return new ArrayLangValue(result, Position);
    }

    /// <summary>
    /// 切片赋值：替换或删除指定范围的元素
    /// 注意：由于数组底层使用固定大小的数组，切片赋值会抛出错误
    /// 建议使用 List 类型来支持切片赋值
    /// </summary>
    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        var length = RunResult.Length;

        // 处理负数索引
        if (start < 0) start += length;
        if (end < 0) end += length;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Max(0, Math.Min(end, length));

        // 确保 start <= end
        if (start > end)
        {
            (start, end) = (end, start);
        }

        var valuesList = values.ToList();
        var rangeLength = end - start;

        // 数组不支持改变大小，因此新值数量必须等于要替换的范围长度
        if (valuesList.Count != rangeLength)
        {
            throw new InvalidOperationError(this,
                $"数组切片赋值不支持改变数组大小。要替换的范围长度为 {rangeLength}，但提供了 {valuesList.Count} 个新值。" +
                $"如果需要改变大小，请使用列表类型 {{...}} 而不是数组类型 [...]");
        }

        // 替换指定范围的元素
        for (int i = 0; i < rangeLength; i++)
        {
            RunResult[start + i] = valuesList[i];
        }
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 创建一个长度为 len 的对象数组
        var len = RunResult.Length;
        ilGenerator.Emit(OpCodes.Ldc_I4, len); // 加载数组长度
        ilGenerator.Emit(OpCodes.Newarr, typeof(object)); // 创建新数组

        for (var i = 0; i < len; i++)
        {
            ilGenerator.Emit(OpCodes.Dup); // 复制数组引用
            ilGenerator.Emit(OpCodes.Ldc_I4, i); // 加载索引

            Type t;
            if (len == Values.Count)
            {
                // 如果Values列表有元素，使用Values[i]
                Values[i].LoadIlValue(ilGenerator, local);
                t = Values[i].OutputType(local)!;
            }
            else
            {
                // 否则，使用RunResult[i]，但要确保它不为null
                var item = RunResult[i];
                if (item == null!)
                {
                    // 如果item为null，加载一个null值
                    ilGenerator.Emit(OpCodes.Ldnull);
                    t = typeof(object);
                }
                else
                {
                    item.LoadIlValue(ilGenerator, local);
                    t = item.OutputType(local)!;
                }
            }

            ilGenerator.Emit(OpCodes.Box, t); // 将值转换为object
            ilGenerator.Emit(OpCodes.Stelem_Ref); // 将值存入数组
        }
    }

    public override Type OutputType(LocalManager local) => typeof(object[]);

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
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