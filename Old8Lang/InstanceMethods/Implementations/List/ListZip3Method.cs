using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Zip3(second, third) - 将三个列表合并为三元组列表
/// </summary>
public class ListZip3Method : BaseInstanceMethod
{
    public override string[] Names => ["Zip3", "zip3"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["second", "third"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var secondValue = parameters[0].Run(manager);
        var thirdValue = parameters[1].Run(manager);

        if (secondValue is not ListLangValue secondList)
        {
            throw new ArgumentError(position, "second 参数必须是列表类型");
        }

        if (thirdValue is not ListLangValue thirdList)
        {
            throw new ArgumentError(position, "third 参数必须是列表类型");
        }

        var result = new List<LangValueType>();
        var minLength = Math.Min(Math.Min(list.Values.Count, secondList.Values.Count), thirdList.Values.Count);

        for (int i = 0; i < minLength; i++)
        {
            var tuple = CreateTupleWithValues(list.Values[i], secondList.Values[i], thirdList.Values[i]);
            result.Add(tuple);
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("List.Zip3 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 支持 List<object?> 和 object?[] 两种类型
        List<object?> list;
        if (instance is List<object?> listInstance)
        {
            list = listInstance;
        }
        else if (instance is object?[] arrayInstance)
        {
            list = arrayInstance.ToList();
        }
        else
        {
            throw new ArgumentException("实例必须是 List<object?> 或 object?[] 类型");
        }

        if (arguments.Length < 2)
        {
            throw new ArgumentException("需要两个参数");
        }

        List<object?> second;
        if (arguments[0] is List<object?> secondList)
        {
            second = secondList;
        }
        else if (arguments[0] is object?[] secondArray)
        {
            second = secondArray.ToList();
        }
        else
        {
            throw new ArgumentException("second 参数必须是列表或数组类型");
        }

        List<object?> third;
        if (arguments[1] is List<object?> thirdList)
        {
            third = thirdList;
        }
        else if (arguments[1] is object?[] thirdArray)
        {
            third = thirdArray.ToList();
        }
        else
        {
            throw new ArgumentException("third 参数必须是列表或数组类型");
        }

        var result = new List<object?>();
        var minLength = Math.Min(Math.Min(list.Count, second.Count), third.Count);

        for (int i = 0; i < minLength; i++)
        {
            result.Add(new object?[] { list[i], second[i], third[i] });
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
