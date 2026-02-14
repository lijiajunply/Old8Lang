using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ToList() - 转换为列表类型
/// </summary>
public class LangListToListMethod : BaseLangListMethod
{
    public override string[] Names => ["ToList", "toList"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(ListLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        // 如果已经是 ListLangValue，直接返回
        if (instance is ListLangValue listValue)
        {
            return listValue;
        }

        // 否则，获取元素并创建新的 ListLangValue
        var items = GetItems(instance);
        return new ListLangValue(items.Select(item => item as LangExpression ?? new LangId(item.ToString() ?? "")).ToList());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListToListMethod).GetMethod(nameof(ToListHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue ToListHelper(ILangList langList)
    {
        if (langList is ListLangValue listValue)
        {
            return listValue;
        }

        var items = langList.GetItems();
        return new ListLangValue(items.Select(item => item as LangExpression ?? new LangId(item.ToString() ?? "")).ToList());
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 在 VM 模式下，列表表示为 object[]
        if (instance is object[] arr)
        {
            return arr; // 已经是数组形式，直接返回
        }

        if (instance is List<object?> list)
        {
            return list.ToArray();
        }

        // 使用基类的辅助方法获取元素列表
        var items = GetItemsForVM(instance);
        return items.ToArray();
    }
}
