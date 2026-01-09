using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 嵌套切片访问表达式，用于处理 matrix[index][start:end] 或 matrix[index][start:end:step] 的情况
/// </summary>
/// <param name="baseExpression">基础表达式，可以是任何返回集合的表达式</param>
/// <param name="sliceStart">切片起始索引</param>
/// <param name="sliceEnd">切片结束索引（可选）</param>
/// <param name="sliceStep">切片步长（可选）</param>
/// <param name="position">源代码位置</param>
public partial class NestedSliceAccess(
    LangExpression baseExpression,
    LangExpression sliceStart,
    LangExpression? sliceEnd = null,
    LangExpression? sliceStep = null,
    SourcePosition position = default)
    : LangValueType(position)
{
    public readonly LangExpression BaseExpression = baseExpression;
    public readonly LangExpression SliceStart = sliceStart;
    public readonly LangExpression? SliceEnd = sliceEnd;
    public readonly LangExpression? SliceStep = sliceStep;

    public override LangValueType Run(VariateManager manager)
    {
        // 首先运行基础表达式，获取结果
        var baseResult = BaseExpression.Run(manager);

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
        var sliceStr = SliceEnd is not null
            ? (SliceStep is not null ? $"{SliceStart}:{SliceEnd}:{SliceStep}" : $"{SliceStart}:{SliceEnd}")
            : (SliceStep is not null ? $"{SliceStart}::{SliceStep}" : $"{SliceStart}:");
        return $"{BaseExpression}[{sliceStr}]";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 首先加载基础表达式的结果
        BaseExpression.LoadIlValue(ilGenerator, local);

        // 如果基础表达式不是object类型，需要装箱
        var baseType = BaseExpression.OutputType(local);
        if (baseType is not null && baseType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, baseType);
        }

        // 加载切片参数 - start
        SliceStart.LoadIlValue(ilGenerator, local);
        var startType = SliceStart.OutputType(local);
        // 转换为int
        if (startType is not null && startType != typeof(int))
        {
            Old8Lang.Compiler.TypeConversion.GenerateTypeConversionIl(ilGenerator, startType, typeof(int), SliceStart);
        }

        // 处理结束索引（可能为null）
        if (SliceEnd is not null)
        {
            SliceEnd.LoadIlValue(ilGenerator, local);
            var endType = SliceEnd.OutputType(local);
            // 转换为int
            if (endType is not null && endType != typeof(int))
            {
                Old8Lang.Compiler.TypeConversion.GenerateTypeConversionIl(ilGenerator, endType, typeof(int), SliceEnd);
            }
        }
        else
        {
            // 如果没有指定结束索引，使用int.MaxValue表示到末尾
            ilGenerator.Emit(OpCodes.Ldc_I4, int.MaxValue);
        }

        // 处理步长（可能为null）
        if (SliceStep is not null)
        {
            SliceStep.LoadIlValue(ilGenerator, local);
            var stepType = SliceStep.OutputType(local);
            // 转换为int
            if (stepType is not null && stepType != typeof(int))
            {
                Old8Lang.Compiler.TypeConversion.GenerateTypeConversionIl(ilGenerator, stepType, typeof(int), SliceStep);
            }
        }
        else
        {
            // 如果没有指定步长，默认为1
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
        }

        // 调用CollectionHelper.Slice方法
        var sliceMethod = typeof(Old8Lang.Compiler.CollectionHelper).GetMethod(
            nameof(Old8Lang.Compiler.CollectionHelper.Slice),
            [typeof(object), typeof(int), typeof(int), typeof(int)]);

        if (sliceMethod is null)
        {
            throw new InvalidOperationError(this, "无法找到CollectionHelper.Slice方法");
        }

        ilGenerator.Emit(OpCodes.Call, sliceMethod);
    }

    public override Type OutputType(LocalManager local)
    {
        var baseType = BaseExpression.OutputType(local);

        // 如果基础类型为null，返回object类型
        if (baseType is null)
        {
            return typeof(object);
        }

        // 切片操作返回与基础类型相同的类型
        // 例如：object[] 切片后仍然是 object[]，List<object> 切片后仍然是 List<object>
        return baseType;
    }
}