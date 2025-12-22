using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 嵌套切片访问表达式，用于处理 matrix[index][start:end] 或 matrix[index][start:end:step] 的情况
/// </summary>
/// <param name="baseIndex">基础索引访问，如 matrix[index]</param>
/// <param name="sliceStart">切片起始索引</param>
/// <param name="sliceEnd">切片结束索引（可选）</param>
/// <param name="sliceStep">切片步长（可选）</param>
/// <param name="position">源代码位置</param>
public partial class NestedSliceAccess(
    LangListItem baseIndex,
    LangExpression sliceStart,
    LangExpression? sliceEnd = null,
    LangExpression? sliceStep = null,
    SourcePosition position = default)
    : LangValueType(position)
{
    public readonly LangListItem BaseIndex = baseIndex;
    public readonly LangExpression SliceStart = sliceStart;
    public readonly LangExpression? SliceEnd = sliceEnd;
    public readonly LangExpression? SliceStep = sliceStep;

    public override LangValueType Run(VariateManager manager)
    {
        // 首先运行基础索引访问，获取结果
        var baseResult = BaseIndex.Run(manager);

        // 将切片参数转换为值
        var startValue = SliceStart.Run(manager);
        var endValue = SliceEnd?.Run(manager);
        var stepValue = SliceStep?.Run(manager);

        var start = startValue.GetValue<int>();
        var end = endValue?.GetValue<int>() ?? int.MaxValue;
        var step = stepValue?.GetValue<int>() ?? 1;

        // 根据基础结果的类型执行切片操作
        if (baseResult is ILangList list)
        {
            return list.Slice(start, end, step);
        }

        throw new InvalidOperationError(this, $"不支持的嵌套切片访问类型: {baseResult.GetType().Name}");
    }

    public override string ToString()
    {
        var sliceStr = SliceEnd != null
            ? (SliceStep != null ? $"{SliceStart}:{SliceEnd}:{SliceStep}" : $"{SliceStart}:{SliceEnd}")
            : (SliceStep != null ? $"{SliceStart}::{SliceStep}" : $"{SliceStart}:");
        return $"{BaseIndex}[{sliceStr}]";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 首先加载基础索引访问的结果
        BaseIndex.LoadIlValue(ilGenerator, local);

        // 加载切片参数
        SliceStart.LoadIlValue(ilGenerator, local);
        SliceEnd?.LoadIlValue(ilGenerator, local);
        SliceStep?.LoadIlValue(ilGenerator, local);

        // 这里需要根据基础结果的类型调用相应的切片方法
        // 具体实现取决于各种类型的 GetSlice 方法签名
        var baseType = BaseIndex.OutputType(local);

        // 调用适当的切片方法
        // 注意：这里需要根据实际的 IL 实现进行调整
        throw new NotImplementedException("NestedSliceAccess 的 IL 生成尚未实现");
    }

    public override Type OutputType(LocalManager local)
    {
        var baseType = BaseIndex.OutputType(local);

        // 切片操作通常返回与基础类型相同的类型
        return baseType;
    }
}