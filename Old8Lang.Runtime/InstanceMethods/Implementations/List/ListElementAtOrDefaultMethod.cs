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
/// List.ElementAtOrDefault(index, defaultValue) - 安全获取指定索引的元素，如果索引越界则返回默认值
/// </summary>
public class ListElementAtOrDefaultMethod : BaseInstanceMethod
{
    public override string[] Names => ["ElementAtOrDefault", "elementAtOrDefault"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["index", "defaultValue"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var indexValue = parameters[0].Run(manager);
        var defaultValue = parameters[1].Run(manager);

        if (indexValue is not IntLangValue indexInt)
        {
            throw new ArgumentError(position, "index 参数必须是整数类型");
        }

        var index = indexInt.Value;

        // 处理负数索引
        if (index < 0) index = list.Values.Count + index;

        // 检查索引是否有效
        if (index < 0 || index >= list.Values.Count)
        {
            return defaultValue;
        }

        return list.Values[index];
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        parameters[1].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListElementAtOrDefaultMethod).GetMethod(nameof(ElementAtOrDefaultHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object? ElementAtOrDefaultHelper(List<object?> list, int index, object? defaultValue)
    {
        // 处理负数索引
        if (index < 0) index = list.Count + index;

        // 检查索引是否有效
        if (index < 0 || index >= list.Count)
        {
            return defaultValue;
        }

        return list[index];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length >= 2)
        {
            if (arguments[0] is not int index)
            {
                throw new ArgumentException("index 参数必须是整数类型");
            }

            var defaultValue = arguments[1];

            // 处理负数索引
            if (index < 0) index = list.Count + index;

            // 检查索引是否有效
            if (index < 0 || index >= list.Count)
            {
                return defaultValue;
            }

            return list[index];
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
