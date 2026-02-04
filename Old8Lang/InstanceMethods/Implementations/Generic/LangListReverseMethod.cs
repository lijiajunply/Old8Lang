using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Reverse() - 反转列表元素顺序
/// 适用于所有实现 ILangList 接口的类型
/// 注意：返回新的 ListLangValue，不修改原列表
/// </summary>
public class LangListReverseMethod : BaseLangListMethod
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
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListReverseMethod).GetMethod(nameof(ReverseHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue ReverseHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        items.Reverse();
        return new ListLangValue(items);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            var items = langList.GetItems().ToList();
            items.Reverse();
            return new ListLangValue(items);
        }

        // 兼容 VM 模式下的 List<object?>
        if (instance is List<object?> list)
        {
            var reversed = new List<object?>(list);
            reversed.Reverse();
            return reversed;
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或为 List<object?> 类型，当前类型：{instance?.GetType().Name}");
    }
}
