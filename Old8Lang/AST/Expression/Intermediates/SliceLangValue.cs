using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 切片表达式
/// </summary>
/// <param name="id"></param>
/// <param name="start"></param>
/// <param name="end"></param>
/// <param name="step"></param>
public partial class SliceLangValue(LangId id, LangExpression? start = null, LangExpression? end = null, LangExpression? step = null) : LangValueType
{
    /// <summary>
    /// 集合标识符
    /// </summary>
    public LangId Id { get; } = id;

    /// <summary>
    /// 起始索引表达式
    /// </summary>
    public LangExpression? Start { get; } = start;

    /// <summary>
    /// 结束索引表达式
    /// </summary>
    public LangExpression? End { get; } = end;

    /// <summary>
    /// 步长表达式
    /// </summary>
    public LangExpression? Step { get; } = step;

    public override LangValueType Run(VariateManager manager)
    {
        var value = Id.Run(manager);
        var start1 = Start?.Run(manager);
        var end1 = End?.Run(manager);
        var step1 = Step?.Run(manager);

        if (value is not ILangList list) throw new InvalidOperationError(this, $"类型 '{value.GetType().Name}' 不支持切片操作");

        var length = list.GetLength();
        var stepValue = step1?.GetValue<int>() ?? 1;

        // 如果步长为0，抛出错误
        if (stepValue == 0)
            throw new InvalidOperationError(this, "切片步长不能为0");

        // 处理负数步长
        int startValue, endValue;
        if (stepValue > 0)
        {
            // 正向切片
            startValue = start1?.GetValue<int>() ?? 0;
            endValue = end1?.GetValue<int>() ?? length;
        }
        else
        {
            // 反向切片
            startValue = start1?.GetValue<int>() ?? length - 1;
            endValue = end1?.GetValue<int>() ?? -1;
        }

        return list.Slice(startValue, endValue, stepValue);
    }

    public override string ToString()
    {
        if (Start != null && End != null && Step != null)
            return $"{Id}[{Start}:{End}:{Step}]";
        if (Start != null && End != null)
            return $"{Id}[{Start}:{End}]";
        if (Start != null)
            return $"{Id}[{Start}:]";
        if (End != null)
            return $"{Id}[:{End}]";
        return $"{Id}[:]"; // Old8Lang 风格的切片表达式
    }
}