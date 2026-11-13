using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Join(separator?) - 连接为字符串
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListJoinMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
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
            var separatorValue = parameters[0].Accept(new InterpreterVisitor(manager));
            if (separatorValue is StringLangValue strValue)
            {
                separator = strValue.Value;
            }
            else
            {
                separator = separatorValue.ToString() ?? ", ";
            }
        }

        var strings = items.Select(item => item.ToDisplayString() ?? "null");
        var result = string.Join(separator, strings);

        return StringLangValue.Create(result, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        if (parameters.Count == 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
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

    /// <summary>IL 模式的辅助方法：用默认分隔符连接桥接后的集合。</summary>
    public static string JoinHelper(LangValueType instance)
    {
        if (instance is not ILangList langList)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        var items = langList.GetItems();
        var strings = items.Select(item => item.ToDisplayString() ?? "null");
        return string.Join(", ", strings);
    }

    /// <summary>IL 模式的辅助方法：使用指定分隔符连接桥接后的集合。</summary>
    public static string JoinWithSeparatorHelper(LangValueType instance, LangValueType separator)
    {
        var separatorText = separator is StringLangValue stringValue
            ? stringValue.Value
            : separator.ToDisplayString() ?? ", ";

        if (instance is not ILangList langList)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        return string.Join(separatorText, langList.GetItems().Select(item => item.ToDisplayString() ?? "null"));
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
            return string.Join(separator, langList.GetItems().Select(item => item.ToDisplayString() ?? "null"));
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
