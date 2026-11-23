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
/// ILangList.Reverse() - 反转列表元素顺序
/// 适用于所有实现 ILangList 接口的类型
/// 注意：返回新的 ListLangValue，不修改原列表
/// </summary>
public class LangListReverseMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Reverse", "reverse"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var reversedItems = new List<LangValueType>(items);
        reversedItems.Reverse();

        // 返回新的 ListLangValue
        return new ListLangValue(reversedItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperName = TargetType == typeof(ArrayLangValue) ? nameof(ReverseArrayHelper) : nameof(ReverseHelper);
        var helperMethod = typeof(LangListReverseMethod).GetMethod(helperName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>IL 模式的辅助方法：反转桥接后的集合并返回原生列表。</summary>
    public static List<object?> ReverseHelper(LangValueType instance)
    {
        if (instance is not ILangList langList)
        {
            throw new ArgumentException($"实例必须实现 ILangList 接口，实际是 {instance.GetType().Name}");
        }

        var items = langList.GetItems().ToList();
        items.Reverse();
        return items.Select(IlValueBridge.Unwrap).ToList();
    }

    /// <summary>IL 模式下反转数组并保持数组返回表示。</summary>
    public static object?[] ReverseArrayHelper(LangValueType instance) => ReverseHelper(instance).ToArray();

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return TargetType == typeof(ArrayLangValue) ? typeof(object?[]) : typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var items = GetItemsForVM(instance).ToList();
        items.Reverse();
        return items;
    }
}
