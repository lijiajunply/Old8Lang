using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Except(other) - 差集
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListExceptMethod : BaseLangListMethod
{
    public override string[] Names => ["Except", "except", "Difference", "difference"];
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
            throw new ArgumentException("Except 方法的参数必须实现 ILangList 接口");
        }

        var otherItems = otherList.GetItems().ToList();
        var exceptItems = new List<LangValueType>();

        // 只保留在 items 中但不在 other 中的元素
        foreach (var item in items)
        {
            var existsInOther = otherItems.Any(other => other.Equal(item));
            var alreadyAdded = exceptItems.Any(existing => existing.Equal(item));

            if (!existsInOther && !alreadyAdded)
            {
                exceptItems.Add(item);
            }
        }

        return new ListLangValue(exceptItems, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListExceptMethod).GetMethod(nameof(ExceptHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue ExceptHelper(ILangList langList, ILangList other)
    {
        var items = langList.GetItems().ToList();
        var otherItems = other.GetItems().ToList();
        var exceptItems = new List<LangValueType>();

        foreach (var item in items)
        {
            var existsInOther = otherItems.Any(o => o.Equal(item));
            var alreadyAdded = exceptItems.Any(existing => existing.Equal(item));

            if (!existsInOther && !alreadyAdded)
            {
                exceptItems.Add(item);
            }
        }

        return new ListLangValue(exceptItems);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList && arguments[0] is ILangList other)
        {
            return ExceptHelper(langList, other);
        }

        if (instance is List<object?> list && arguments[0] is List<object?> otherList)
        {
            return list.Except(otherList).ToList();
        }

        throw new ArgumentException($"实例和参数必须实现 ILangList 接口或为 List<object?> 类型");
    }
}
