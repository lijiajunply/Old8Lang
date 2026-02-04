using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.ElementAt(index) - 获取指定索引的元素
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListElementAtMethod : BaseLangListMethod
{
    public override string[] Names => ["ElementAt", "elementAt", "At", "at"];
    public override string[] ParameterNames => ["index"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var indexValue = parameters[0].Run(manager);

        if (indexValue is not IntLangValue index)
        {
            throw new TypeError(parameters[0], "IntValue", indexValue.GetType().Name);
        }

        int idx = index.Value;

        // 支持负数索引
        if (idx < 0)
        {
            idx = items.Count + idx;
        }

        if (idx < 0 || idx >= items.Count)
        {
            throw new IndexError(instance, idx, items.Count);
        }

        return items[idx];
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListElementAtMethod).GetMethod(nameof(ElementAtHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static LangValueType ElementAtHelper(ILangList langList, int index)
    {
        var items = langList.GetItems().ToList();
        int idx = index;

        if (idx < 0)
        {
            idx = items.Count + idx;
        }

        if (idx < 0 || idx >= items.Count)
        {
            throw new IndexOutOfRangeException($"索引 {index} 超出范围");
        }

        return items[idx];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList && arguments[0] is int index)
        {
            return ElementAtHelper(langList, index);
        }

        if (instance is System.Collections.IList list && arguments[0] is int idx)
        {
            if (idx < 0)
            {
                idx = list.Count + idx;
            }
            return list[idx];
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口或 IList 接口，当前类型：{instance?.GetType().Name}");
    }
}
