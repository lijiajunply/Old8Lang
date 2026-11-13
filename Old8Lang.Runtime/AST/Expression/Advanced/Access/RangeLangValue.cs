using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

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

        // 根据包含规则调整起始值
        var startNum = start;
        var endNum = end;

        if (!includeStart)
            startNum++;
        if (!includeEnd)
            endNum--;

        // 与解释器一致：端点排除后的边界决定方向。比如 [1>~<1]
        // 调整为 2~0，应产生 2, 1, 0，而不是因为原始端点相等而返回空数组。
        if (startNum > endNum)
        {
            // 反向范围:从大到小
            for (var i = startNum; i >= endNum; i--)
            {
                results.Add(i);
            }
        }
        else if (startNum <= endNum)
        {
            // 正向范围:从小到大
            for (var i = startNum; i <= endNum; i++)
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
