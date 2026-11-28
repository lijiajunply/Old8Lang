using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 范围表达式，创建整数或字符数组
/// </summary>
/// <param name="start"></param>
/// <param name="end"></param>
/// <param name="position"></param>
/// <param name="includeStart">是否包含起始值</param>
/// <param name="includeEnd">是否包含结束值</param>
/// <param name="step">可选的步长表达式</param>
public partial class RangeLangValue(
    LangExpression? start,
    LangExpression? end,
    SourcePosition position = default,
    bool includeStart = true,
    bool includeEnd = true,
    LangExpression? step = null) : LangValueType(position)
{

    /// <summary>
    /// 起始表达式
    /// </summary>
    public LangExpression? Start { get; } = start;

    /// <summary>
    /// 结束表达式
    /// </summary>
    public LangExpression? End { get; } = end;

    public LangExpression? Step { get; } = step;

    public bool IncludeStart { get; } = includeStart;
    public bool IncludeEnd { get; } = includeEnd;

    /// <summary>创建单位步长的整数范围。</summary>
    public static int[] CreateRangeArray(int start, int end, bool includeStart, bool includeEnd) =>
        CreateSteppedRangeArray(start, end, 1, includeStart, includeEnd);

    /// <summary>创建指定步长的整数范围。</summary>
    public static int[] CreateSteppedRangeArray(int start, int end, int step, bool includeStart, bool includeEnd) =>
        CreateElements(start, end, step, includeStart, includeEnd, static value => value);

    /// <summary>按 UTF-16 码元顺序创建字符范围。</summary>
    public static char[] CreateCharacterRangeArray(char start, char end, int step, bool includeStart, bool includeEnd) =>
        CreateElements(start, end, step, includeStart, includeEnd, static value => (char)value);

    internal static Array CreateRange(object? start, object? end, object? step, bool includeStart, bool includeEnd,
        SourcePosition position)
    {
        var stride = RequireRangeStep(step, position);
        return (start, end) switch
        {
            (int first, int last) => CreateElements(first, last, stride, includeStart, includeEnd,
                static value => value, position),
            (char first, char last) => CreateElements(first, last, stride, includeStart, includeEnd,
                static value => (char)value, position),
            _ => throw new TypeError(position, "范围端点必须同为整数或同为字符")
        };
    }

    /// <summary>校验 IL 模式下运行期确定类型的步长。</summary>
    public static int RequireRangeStep(object? step) => RequireRangeStep(step, default);

    private static int RequireRangeStep(object? step, SourcePosition position) =>
        step is int stride ? stride : throw new TypeError(position, "范围步长必须是整数");

    private static T[] CreateElements<T>(int start, int end, int step, bool includeStart, bool includeEnd,
        Func<int, T> convert, SourcePosition position = default)
    {
        if (step <= 0)
            throw new InvalidOperationError(position, "范围步长必须大于 0");

        // 使用 long 计算距离与偏移，避免触及 int 边界时回绕。
        var distance = Math.Abs((long)end - start) - (includeEnd ? 0 : 1);
        var firstOffset = includeStart ? 0 : step;
        if (distance < firstOffset)
            return [];

        var count = (distance - firstOffset) / step + 1;
        if (count > Array.MaxLength)
            throw new OutOfMemoryError(position, "范围元素数超过数组长度上限");

        var direction = start <= end ? 1 : -1;
        var results = new T[(int)count];
        var offset = (long)firstOffset;
        for (var index = 0; index < results.Length; index++, offset += step)
            results[index] = convert((int)(start + direction * offset));

        return results;
    }

    public override string ToString()
    {
        var startSymbol = IncludeStart ? "[" : "(";
        var endSymbol = IncludeEnd ? "]" : ")";
        return $"{startSymbol}{Start}~{End}{(Step is null ? "" : $":{Step}")}{endSymbol}";
    }
}
