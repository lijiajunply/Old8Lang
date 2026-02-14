using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ToDict() - 转换为字典类型
/// 要求列表元素是键值对（元组或包含两个元素的列表）
/// </summary>
public class LangListToDictMethod : BaseLangListMethod
{
    public override string[] Names => ["ToDict", "toDict"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(DictionaryLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        // 如果已经是 DictionaryLangValue，直接返回
        if (instance is DictionaryLangValue dictValue)
        {
            return dictValue;
        }

        // 获取元素并创建新的 DictionaryLangValue
        var items = GetItems(instance);
        var tuples = new List<TupleLangValue>();

        foreach (var item in items)
        {
            // 每个元素应该是一个包含两个元素的元组或列表
            if (item is TupleLangValue tuple)
            {
                tuples.Add(tuple);
            }
            else if (item is ILangList list)
            {
                var listItems = list.GetItems().ToList();
                if (listItems.Count >= 2)
                {
                    var key = listItems[0] as LangExpression ?? new LangId(listItems[0].ToString() ?? "");
                    var value = listItems[1] as LangExpression ?? new LangId(listItems[1].ToString() ?? "");
                    tuples.Add(new TupleLangValue(key, value));
                }
            }
            else
            {
                throw new InvalidOperationException($"ToDict 要求列表元素是键值对（元组或包含两个元素的列表），但得到了 {item.GetType().Name}");
            }
        }

        return new DictionaryLangValue(tuples);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListToDictMethod).GetMethod(nameof(ToDictHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static DictionaryLangValue ToDictHelper(ILangList langList)
    {
        if (langList is DictionaryLangValue dictValue)
        {
            return dictValue;
        }

        var items = langList.GetItems();
        var tuples = new List<TupleLangValue>();

        foreach (var item in items)
        {
            if (item is TupleLangValue tuple)
            {
                tuples.Add(tuple);
            }
            else if (item is ILangList list)
            {
                var listItems = list.GetItems().ToList();
                if (listItems.Count >= 2)
                {
                    var key = listItems[0] as LangExpression ?? new LangId(listItems[0].ToString() ?? "");
                    var value = listItems[1] as LangExpression ?? new LangId(listItems[1].ToString() ?? "");
                    tuples.Add(new TupleLangValue(key, value));
                }
            }
            else
            {
                throw new InvalidOperationException($"ToDict 要求列表元素是键值对（元组或包含两个元素的列表），但得到了 {item.GetType().Name}");
            }
        }

        return new DictionaryLangValue(tuples);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(DictionaryLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 在 VM 模式下，字典表示为 Dictionary<object, object?>
        if (instance is Dictionary<object, object?> existingDict)
        {
            return existingDict;
        }

        // 使用基类的辅助方法获取元素列表
        var items = GetItemsForVM(instance);
        var dict = new Dictionary<object, object?>();

        foreach (var item in items)
        {
            // 每个元素应该是一个包含两个元素的元组或数组
            if (item is Tuple<object?, object?> tuple)
            {
                if (tuple.Item1 != null)
                {
                    dict[tuple.Item1] = tuple.Item2;
                }
            }
            else if (item is object[] arr && arr.Length >= 2)
            {
                if (arr[0] != null)
                {
                    dict[arr[0]] = arr[1];
                }
            }
            else if (item is List<object?> list && list.Count >= 2)
            {
                if (list[0] != null)
                {
                    dict[list[0]] = list[1];
                }
            }
            else
            {
                throw new InvalidOperationException($"ToDict 要求列表元素是键值对（元组或包含两个元素的列表）");
            }
        }

        return dict;
    }
}
