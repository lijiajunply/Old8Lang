namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 范围表达式，创建一个整数数组
/// </summary>
/// <param name="start"></param>
/// <param name="end"></param>
/// <param name="position"></param>
/// <param name="includeStart">是否包含起始值</param>
/// <param name="includeEnd">是否包含结束值</param>
public partial class RangeLangValue(
    LangExpression? start,
    LangExpression? end,
    SourcePosition position = default,
    bool includeStart = true,
    bool includeEnd = true) : LangValueType(position)
{

    /// <summary>
    /// 起始表达式
    /// </summary>
    public LangExpression? Start { get; } = start;

    /// <summary>
    /// 结束表达式
    /// </summary>
    public LangExpression? End { get; } = end;

    public bool IncludeStart { get; } = includeStart;
    public bool IncludeEnd { get; } = includeEnd;

    /// <summary>
    /// 创建范围数组的辅助方法(用于 IL 模式)
    /// </summary>
    public static int[] CreateRangeArray(int start, int end, bool includeStart, bool includeEnd)
    {
        var results = new List<int>();

        // 方向由**原始端点**决定，排除端点只会收窄区间，不能把方向翻转。
        // 例如 [5>~<5] 的区间是 (5, 5)，应为空；此前先 +1/-1 得到 6~4，
        // 再按 6 > 4 判成反向范围，错误地产生 [6, 5, 4]。
        if (start <= end)
        {
            var lo = includeStart ? start : start + 1;
            var hi = includeEnd ? end : end - 1;
            for (var i = lo; i <= hi; i++)
            {
                results.Add(i);
            }
        }
        else
        {
            var hi = includeStart ? start : start - 1;
            var lo = includeEnd ? end : end + 1;
            for (var i = hi; i >= lo; i--)
            {
                results.Add(i);
            }
        }

        return results.ToArray();
    }

    public override string ToString()
    {
        var startSymbol = IncludeStart ? "[" : "(";
        var endSymbol = IncludeEnd ? "]" : ")";
        return $"{startSymbol}{Start}~{End}{endSymbol}";
    }
}
