using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
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

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载集合（id）
        Id.LoadIlValue(ilGenerator, local);

        // 加载起始索引
        if (Start is not null)
        {
            Start.LoadIlValue(ilGenerator, local);
            var startType = Start.OutputType(local);
            if (startType is not null && startType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, startType, typeof(int), Start);
            }
        }
        else
        {
            // 默认起始索引为 0
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
        }

        // 加载结束索引
        if (End is not null)
        {
            End.LoadIlValue(ilGenerator, local);
            var endType = End.OutputType(local);
            if (endType is not null && endType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, endType, typeof(int), End);
            }
        }
        else
        {
            // 默认结束索引为 int.MaxValue (表示到末尾)
            ilGenerator.Emit(OpCodes.Ldc_I4, int.MaxValue);
        }

        // 加载步长
        if (Step is not null)
        {
            Step.LoadIlValue(ilGenerator, local);
            var stepType = Step.OutputType(local);
            if (stepType is not null && stepType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, stepType, typeof(int), Step);
            }
        }
        else
        {
            // 默认步长为 1
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
        }

        // 调用 CollectionHelper.Slice 方法
        var sliceMethod = typeof(CollectionHelper).GetMethod(
            nameof(CollectionHelper.Slice),
            [typeof(object), typeof(int), typeof(int), typeof(int)]);

        if (sliceMethod is null)
        {
            throw new InvalidOperationError(this, "无法找到CollectionHelper.Slice方法");
        }

        ilGenerator.Emit(OpCodes.Call, sliceMethod);
    }

    public override Type OutputType(LocalManager local)
    {
        // 获取 Id 的类型
        var idType = Id.OutputType(local);

        // 如果类型为 null，返回 object 类型
        if (idType is null)
        {
            return typeof(object);
        }

        // 切片操作返回与原类型相同的类型
        return idType;
    }

    public override string ToString()
    {
        if (Start is not null && End is not null && Step is not null)
            return $"{Id}[{Start}:{End}:{Step}]";
        if (Start is not null && End is not null)
            return $"{Id}[{Start}:{End}]";
        if (Start is not null)
            return $"{Id}[{Start}:]";
        if (End is not null)
            return $"{Id}[:{End}]";
        return $"{Id}[:]"; // Old8Lang 风格的切片表达式
    }
}