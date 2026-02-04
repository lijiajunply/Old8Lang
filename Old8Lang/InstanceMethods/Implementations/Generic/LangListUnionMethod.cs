using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Union(other) - 并集
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListUnionMethod : BaseLangListMethod
{
    public override string[] Names => ["Union", "union"];
    public override string[] ParameterNames => ["other"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var otherValue = parameters[0].Run(manager);

        if (otherValue is not ILangList otherList)
        {
            throw new ArgumentException("Union 方法的参数必须实现 ILangList 接口");
        }

        var otherItems = otherList.GetItems();
        var unionItems = new List<LangValueType>(items);

        // 添加 other 中不在 items 中的元素
        foreach (var item in otherItems)
        {
            var exists = unionItems.Any(existing => existing.Equal(item));
            if (!exists)
            {
                unionItems.Add(item);
            }
        }

        return new ListLangValue(unionItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListUnionMethod).GetMethod(nameof(UnionHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue UnionHelper(ILangList langList, ILangList other)
    {
        var items = langList.GetItems().ToList();
        var otherItems = other.GetItems();
        var unionItems = new List<LangValueType>(items);

        foreach (var item in otherItems)
        {
            var exists = unionItems.Any(existing => existing.Equal(item));
            if (!exists)
            {
                unionItems.Add(item);
            }
        }

        return new ListLangValue(unionItems);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList && arguments[0] is ILangList other)
        {
            return UnionHelper(langList, other);
        }

        if (instance is List<object?> list && arguments[0] is List<object?> otherList)
        {
            return list.Union(otherList).ToList();
        }

        throw new ArgumentException($"实例和参数必须实现 ILangList 接口或为 List<object?> 类型");
    }
}
