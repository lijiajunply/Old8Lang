using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Distinct() - 去重
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListDistinctMethod : BaseLangListMethod
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
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListDistinctMethod).GetMethod(nameof(DistinctHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue DistinctHelper(ILangList langList)
    {
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

        return new ListLangValue(distinctItems);
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
            var distinctItems = new List<LangValueType>();

            foreach (var item in items)
            {
                var exists = distinctItems.Any(existing => existing.Equal(item));
                if (!exists)
                {
                    distinctItems.Add(item);
                }
            }

            return new ListLangValue(distinctItems);
        }

        if (instance is List<object?> list)
        {
            return list.Distinct().ToList();
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或为 List<object?> 类型，当前类型：{instance?.GetType().Name}");
    }
}
