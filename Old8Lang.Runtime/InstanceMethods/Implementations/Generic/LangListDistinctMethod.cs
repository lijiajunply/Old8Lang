using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Distinct() - 去重
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListDistinctMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Distinct", "distinct", "Unique", "unique"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var distinctItems = new List<LangValueType>();

        foreach (var item in items)
        {
            // 检查是否已存在
            var exists = distinctItems.Any(existing => existing.Equal(item));
            if (!exists)
            {
                distinctItems.Add(item);
            }
        }

        return new ListLangValue(distinctItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperName = TargetType == typeof(ArrayLangValue) ? nameof(DistinctArrayHelper) : nameof(DistinctHelper);
        var helperMethod = typeof(LangListDistinctMethod).GetMethod(helperName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>IL 模式的辅助方法：对桥接后的集合去重并返回原生列表。</summary>
    public static List<object?> DistinctHelper(LangValueType instance)
    {
        if (instance is not ILangList langList)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        var items = langList.GetItems().ToList();
        var distinctItems = new List<LangValueType>();

        foreach (var item in items)
        {
            var exists = distinctItems.Any(existing => existing.Equal(item));
            if (!exists)
            {
                distinctItems.Add(item);
            }
        }

        return distinctItems.Select(IlValueBridge.Unwrap).ToList();
    }

    /// <summary>IL 模式下数组去重并保持数组返回表示。</summary>
    public static object?[] DistinctArrayHelper(LangValueType instance) => DistinctHelper(instance).ToArray();

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return TargetType == typeof(ArrayLangValue) ? typeof(object?[]) : typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            return new ListLangValue(DistinctHelper((LangValueType)langList).Select(IlValueBridge.Wrap).ToList());
        }

        if (instance is List<object?> list)
        {
            return list.Distinct().ToList();
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或为 List<object?> 类型，当前类型：{instance?.GetType().Name}");
    }
}
