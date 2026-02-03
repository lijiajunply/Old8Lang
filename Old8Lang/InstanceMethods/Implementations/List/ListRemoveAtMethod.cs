using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.RemoveAt 方法 - 根据索引从列表中移除元素
/// </summary>
public class ListRemoveAtMethod : BaseInstanceMethod
{
    public override string[] Names => ["RemoveAt", "removeAt"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["index"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var indexParam = parameters[0].Run(manager);

        if (indexParam is not IntLangValue indexValue)
        {
            throw new Error.TypeError(position, $"RemoveAt 方法的参数必须是整数类型，但实际是 {indexParam.GetType().Name}");
        }

        var index = indexValue.Value;
        if (index < 0 || index >= list.Values.Count)
        {
            throw new Error.IndexError(list, index, list.Values.Count);
        }

        var removed = list.Values[index];
        list.Values.RemoveAt(index);
        return removed;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 获取 Values 字段
        var valuesField = typeof(ListLangValue).GetField("Values");
        ilGenerator.Emit(OpCodes.Ldfld, valuesField!);

        // 加载索引参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var removeAtHelperMethod = typeof(ListRemoveAtMethod).GetMethod(nameof(RemoveAtHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, removeAtHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：根据索引移除元素
    /// </summary>
    public static LangValueType RemoveAtHelper(List<LangValueType> list, LangValueType indexParam)
    {
        if (indexParam is not IntLangValue indexValue)
        {
            throw new Exception($"RemoveAt 方法的参数必须是整数类型");
        }

        var index = indexValue.Value;
        if (index < 0 || index >= list.Count)
        {
            throw new Exception($"索引 {index} 超出范围");
        }

        var removed = list[index];
        list.RemoveAt(index);
        return removed;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (arguments[0] is not int index)
            {
                throw new ArgumentException("RemoveAt 方法的参数必须是整数类型");
            }

            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index} 超出范围");
            }

            var removed = list[index];
            list.RemoveAt(index);
            return removed;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
