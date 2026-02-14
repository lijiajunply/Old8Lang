using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ToTuple() - 转换为元组类型
/// </summary>
public class LangListToTupleMethod : BaseLangListMethod
{
    public override string[] Names => ["ToTuple", "toTuple"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(TupleLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        // 如果已经是 TupleLangValue，直接返回
        if (instance is TupleLangValue tupleValue)
        {
            return tupleValue;
        }

        // 否则，获取元素并创建新的 TupleLangValue
        var items = GetItems(instance).ToList();

        if (items.Count == 0)
        {
            // 空元组
            return new TupleLangValue(new LangId("null"), new LangId("null"));
        }

        if (items.Count == 1)
        {
            // 单元素元组
            return new TupleLangValue(items[0] as LangExpression ?? new LangId(items[0].ToString() ?? ""), new LangId("null"));
        }

        // 构建嵌套元组：(a, (b, (c, d)))
        LangExpression current = items[^1] as LangExpression ?? new LangId(items[^1].ToString() ?? "");
        for (int i = items.Count - 2; i >= 0; i--)
        {
            var item = items[i] as LangExpression ?? new LangId(items[i].ToString() ?? "");
            current = new TupleLangValue(item, current);
        }

        return (TupleLangValue)current;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListToTupleMethod).GetMethod(nameof(ToTupleHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static TupleLangValue ToTupleHelper(ILangList langList)
    {
        if (langList is TupleLangValue tupleValue)
        {
            return tupleValue;
        }

        var items = langList.GetItems().ToList();

        if (items.Count == 0)
        {
            return new TupleLangValue(new LangId("null"), new LangId("null"));
        }

        if (items.Count == 1)
        {
            return new TupleLangValue(items[0] as LangExpression ?? new LangId(items[0].ToString() ?? ""), new LangId("null"));
        }

        // 构建嵌套元组
        LangExpression current = items[^1] as LangExpression ?? new LangId(items[^1].ToString() ?? "");
        for (int i = items.Count - 2; i >= 0; i--)
        {
            var item = items[i] as LangExpression ?? new LangId(items[i].ToString() ?? "");
            current = new TupleLangValue(item, current);
        }

        return (TupleLangValue)current;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TupleLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 在 VM 模式下，元组表示为 Tuple<object?, object?>
        // 如果已经是元组，直接返回
        if (instance?.GetType().IsGenericType == true &&
            instance.GetType().GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            return instance;
        }

        // 使用基类的辅助方法获取元素列表
        var items = GetItemsForVM(instance);

        if (items.Count == 0)
        {
            return new Tuple<object?, object?>(null, null);
        }

        if (items.Count == 1)
        {
            return new Tuple<object?, object?>(items[0], null);
        }

        // 构建嵌套元组：(a, (b, (c, d)))
        object? current = items[^1];
        for (int i = items.Count - 2; i >= 0; i--)
        {
            current = new Tuple<object?, object?>(items[i], current);
        }

        return current;
    }
}
