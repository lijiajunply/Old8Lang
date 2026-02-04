using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.ElementAt 方法 - 返回指定索引处的元素
/// </summary>
public class ListElementAtMethod : BaseInstanceMethod
{
    public override string[] Names => ["ElementAt", "elementAt", "At", "at"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["index"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var indexValue = parameters[0].Run(manager);

        if (indexValue is not IntLangValue index)
        {
            throw new ArgumentError(position, "索引必须是整数类型");
        }

        if (index.Value < 0 || index.Value >= list.Values.Count)
        {
            throw new ArgumentError(position, $"索引 {index.Value} 超出范围 [0, {list.Values.Count})");
        }

        return list.Values[index.Value];
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载索引
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListElementAtMethod).GetMethod(nameof(ElementAtHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取指定索引的元素
    /// </summary>
    public static LangValueType ElementAtHelper(ListLangValue list, IntLangValue index)
    {
        if (index.Value < 0 || index.Value >= list.Values.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index.Value} 超出范围 [0, {list.Values.Count})");
        }

        return list.Values[index.Value];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (arguments[0] is int index)
            {
                if (index < 0 || index >= list.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index} 超出范围 [0, {list.Count})");
                }

                return list[index];
            }

            throw new ArgumentException("索引必须是整数类型");
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
