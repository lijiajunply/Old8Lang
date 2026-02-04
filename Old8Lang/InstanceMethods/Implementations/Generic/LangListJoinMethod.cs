using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Join(separator?) - 连接为字符串
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListJoinMethod : BaseLangListMethod
{
    public override string[] Names => ["Join", "join"];
    public override string[]? ParameterNames => ["separator"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);

        string separator = ", ";
        if (parameters.Count == 1)
        {
            var separatorValue = parameters[0].Run(manager);
            if (separatorValue is StringLangValue strValue)
            {
                separator = strValue.Value;
            }
            else
            {
                separator = separatorValue.ToString() ?? ", ";
            }
        }

        var strings = items.Select(item => item.ToString() ?? "null");
        var result = string.Join(separator, strings);

        return StringLangValue.Create(result, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        if (parameters.Count == 1)
        {
            parameters[0].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(LangListJoinMethod).GetMethod(nameof(JoinWithSeparatorHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(LangListJoinMethod).GetMethod(nameof(JoinHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static string JoinHelper(ILangList langList)
    {
        var items = langList.GetItems();
        var strings = items.Select(item => item.ToString() ?? "null");
        return string.Join(", ", strings);
    }

    public static string JoinWithSeparatorHelper(ILangList langList, string separator)
    {
        var items = langList.GetItems();
        var strings = items.Select(item => item.ToString() ?? "null");
        return string.Join(separator, strings);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        string separator = arguments.Length > 0 ? arguments[0]?.ToString() ?? ", " : ", ";

        if (instance is ILangList langList)
        {
            return JoinWithSeparatorHelper(langList, separator);
        }

        // 支持 object[] 类型
        if (instance is object?[] array)
        {
            return string.Join(separator, array.Select(item => FormatItem(item)));
        }

        // 支持 List<object?> 类型
        if (instance is List<object?> list)
        {
            return string.Join(separator, list.Select(item => FormatItem(item)));
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口、为数组类型或为 List<object?> 类型，当前类型：{instance?.GetType().Name}");
    }

    /// <summary>
    /// 格式化单个元素，特殊处理数组和元组
    /// </summary>
    private static string FormatItem(object? item)
    {
        if (item == null)
        {
            return "null";
        }

        // 如果是 object[] (VM 模式下的元组或数组)
        if (item is object?[] arr)
        {
            var formatted = arr.Select(FormatItem);
            return $"[{string.Join(", ", formatted)}]";
        }

        // 如果是 Tuple<object?, object?> (VM 模式下的元组)
        if (item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            var items = FlattenTuple(item);
            var formatted = items.Select(FormatItem);
            return $"[{string.Join(", ", formatted)}]";
        }

        return item.ToString() ?? "null";
    }

    /// <summary>
    /// 将嵌套的 Tuple<object?, object?> 展平为列表
    /// </summary>
    private static List<object?> FlattenTuple(object tuple)
    {
        var result = new List<object?>();

        if (tuple is not Tuple<object?, object?> t)
        {
            return result;
        }

        result.Add(t.Item1);

        if (t.Item2?.GetType().IsGenericType == true &&
            t.Item2.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            result.AddRange(FlattenTuple(t.Item2));
        }
        else if (t.Item2 != null)
        {
            result.Add(t.Item2);
        }

        return result;
    }
}
