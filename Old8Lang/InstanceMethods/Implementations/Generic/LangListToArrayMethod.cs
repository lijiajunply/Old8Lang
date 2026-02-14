using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ToArray() - 转换为数组类型
/// </summary>
public class LangListToArrayMethod : BaseLangListMethod
{
    public override string[] Names => ["ToArray", "toArray"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(ArrayLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        // 如果已经是 ArrayLangValue，直接返回
        if (instance is ArrayLangValue arrayValue)
        {
            return arrayValue;
        }

        // 否则，获取元素并创建新的 ArrayLangValue
        var items = GetItems(instance);
        return new ArrayLangValue(items.ToList());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListToArrayMethod).GetMethod(nameof(ToArrayHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ArrayLangValue ToArrayHelper(ILangList langList)
    {
        if (langList is ArrayLangValue arrayValue)
        {
            return arrayValue;
        }

        var items = langList.GetItems();
        return new ArrayLangValue(items.ToList());
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ArrayLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 在 VM 模式下，数组表示为 object[]
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
