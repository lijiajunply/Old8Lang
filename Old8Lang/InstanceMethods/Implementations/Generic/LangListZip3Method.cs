using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Zip3(second, third) - 将三个列表合并为三元组列表
/// </summary>
public class LangListZip3Method : BaseLangListMethod
{
    public override string[] Names => ["Zip3", "zip3"];
    public override string[] ParameterNames => ["second", "third"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var secondValue = parameters[0].Run(manager);
        var thirdValue = parameters[1].Run(manager);

        if (!IsLangList(secondValue))
        {
            throw new ArgumentError(position, "second 参数必须是列表或数组类型");
        }

        if (!IsLangList(thirdValue))
        {
            throw new ArgumentError(position, "third 参数必须是列表或数组类型");
        }

        var secondItems = GetItems(secondValue);
        var thirdItems = GetItems(thirdValue);

        var result = new List<LangValueType>();
        var minLength = Math.Min(Math.Min(items.Count, secondItems.Count), thirdItems.Count);

        for (int i = 0; i < minLength; i++)
        {
            var tuple = CreateTupleWithValues(items[i], secondItems[i], thirdItems[i]);
            result.Add(tuple);
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Zip3 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var items = GetItemsForVM(instance);

        if (arguments.Length < 2)
        {
            throw new ArgumentException("需要两个参数");
        }

        var secondItems = GetItemsForVM(arguments[0]);
        var thirdItems = GetItemsForVM(arguments[1]);

        var result = new List<object?>();
        var minLength = Math.Min(Math.Min(items.Count, secondItems.Count), thirdItems.Count);

        for (int i = 0; i < minLength; i++)
        {
            result.Add(new object?[] { items[i], secondItems[i], thirdItems[i] });
        }

        return result;
    }

    /// <summary>
    /// 创建一个带有预填充 ItemValues 的 TupleLangValue
    /// </summary>
    private static TupleLangValue CreateTupleWithValues(params LangValueType[] values)
    {
        var tuple = new TupleLangValue(values.Cast<LangExpression>().ToList());
        foreach (var value in values)
        {
            tuple.ItemValues.Add(value);
        }
        return tuple;
    }
}
