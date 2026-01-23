using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
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

    public override LangValueType Run(VariateManager manager)
    {
        var results = new List<LangValueType>();

        var startValue = Start?.Run(manager);
        var endValue = End?.Run(manager);

        if (startValue is not IntLangValue startIntValue || endValue is not IntLangValue endIntValue)
            throw new TypeError(this, "IntValue",
                $"RangeValue: start 或 end 不是 IntValue，实际得到了 {startValue?.GetType().Name} 和 {endValue?.GetType().Name}");

        // 根据包含规则调整起始值
        var startNum = startIntValue.Value;
        var endNum = endIntValue.Value;

        if (!IncludeStart)
            startNum++;
        if (!IncludeEnd)
            endNum--;

        // 检查范围是否有效
        if (startNum > endNum)
        {
            for (var i = startNum; i >= endNum; i--)
            {
                results.Add(new IntLangValue(i));
            }
        }
        else
        {
            for (var i = startNum; i <= endNum; i++)
            {
                results.Add(new IntLangValue(i));
            }
        }

        return new ArrayLangValue(results);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 我们需要生成与解释器相同的行为：
        // 1. 根据 includeStart 和 includeEnd 调整范围
        // 2. 检查是正向还是反向
        // 3. 生成相应的数组

        // 为了简化，我们直接调用一个辅助方法来生成范围数组
        // 加载起始值
        Start?.LoadIlValue(ilGenerator, local);

        // 加载结束值
        End?.LoadIlValue(ilGenerator, local);

        // 加载 includeStart
        ilGenerator.Emit(IncludeStart ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        // 加载 includeEnd
        ilGenerator.Emit(IncludeEnd ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        // 调用辅助方法 CreateRangeArray(int start, int end, bool includeStart, bool includeEnd)
        var createRangeMethod = typeof(RangeLangValue).GetMethod(nameof(CreateRangeArray),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

        ilGenerator.Emit(OpCodes.Call, createRangeMethod);
    }

    /// <summary>
    /// 创建范围数组的辅助方法(用于编译模式)
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

        // 检查范围是否有效
        // 如果start原本就大于end,说明是反向范围
        if (start > end)
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
        // 如果调整后startNum > endNum但原本start <= end,说明排除导致范围为空,返回空数组

        return results.ToArray();
    }

    public override Type OutputType(LocalManager local) => typeof(int[]);

    public override string ToString()
    {
        var startSymbol = IncludeStart ? "[" : "(";
        var endSymbol = IncludeEnd ? "]" : ")";
        return $"{startSymbol}{Start}~{End}{endSymbol}";
    }
}