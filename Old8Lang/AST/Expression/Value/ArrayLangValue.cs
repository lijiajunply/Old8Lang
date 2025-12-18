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
    private readonly LangValueType[] RunResult;
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

            // 类型检查和转换：确保添加的元素类型与数组中已有的元素类型一致
            // 如果类型不一致，尝试进行类型转换
            LangValueType convertedValue = value;
            if (RunResult.Length > 0 && RunResult[idx] != null!)
            {
                var existingType = RunResult[idx].TypeToString().ToLower();
                var newValueType = value.TypeToString().ToLower();

                if (existingType != newValueType)
                {
                    // 尝试进行类型转换
                    try
                    {
                        // 创建类型值用于转换
                        var targetType = new TypeLangValue(existingType);
                        // 调用 Converse 方法进行类型转换
                        convertedValue = value.Converse(targetType, new VariateManager());
                    }
                    catch (Exception e)
                    {
                        // 如果转换失败，抛出类型不匹配错误
                        throw new TypeError(this, existingType, newValueType,
                            $"数组元素类型必须一致，无法将 {newValueType} 转换为 {existingType}: {e.Message}");
                    }
                }
            }
            else if (RunResult.Length > 0)
            {
                // 如果数组元素为空，检查其他非空元素的类型
                foreach (var t in RunResult)
                {
                    if (t == null!) continue;
                    var existingType = t.TypeToString().ToLower();
                    var newValueType = value.TypeToString().ToLower();

                    if (existingType != newValueType)
                    {
                        // 尝试进行类型转换
                        try
                        {
                            // 创建类型值用于转换
                            var targetType = new TypeLangValue(existingType);
                            // 调用 Converse 方法进行类型转换
                            convertedValue = value.Converse(targetType, new VariateManager());
                        }
                        catch (Exception e)
                        {
                            // 如果转换失败，抛出类型不匹配错误
                            throw new TypeError(this, existingType, newValueType,
                                $"数组元素类型必须一致，无法将 {newValueType} 转换为 {existingType}: {e.Message}");
                        }
                    }

                    break;
                }
            }

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

    // 覆盖 Dot 方法以支持嵌套索引访问，如 array[0][0]
    public override LangValueType Dot(LangExpression dotExpression)
    {
        // 如果 dotExpression 是一个整数值或可以转换为整数的表达式，则视为索引访问
        if (dotExpression is IntLangValue intValue)
        {
            return Get(intValue);
        }

        // 如果是其他类型的表达式，尝试将其作为索引（可能需要运行表达式）
        // 这里需要一个 manager，但我们没有，所以使用一个临时的
        var tempManager = new VariateManager();
        var result = dotExpression.Run(tempManager);

        if (result is IntLangValue idx)
        {
            return Get(idx);
        }

        // 如果不是索引访问，调用父类的 Dot 方法（会报错）
        return base.Dot(dotExpression);
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

    public LangValueType Slice(int start, int end)
    {
        if (start < 0) start += RunResult.Length;
        if (end < 0) end += RunResult.Length + 1;
        // 使用接受 List<LangValueType> 的构造函数，因为 RunResult 已经包含了运行后的值
        return new ArrayLangValue(RunResult[start..end].ToList(), Position);
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