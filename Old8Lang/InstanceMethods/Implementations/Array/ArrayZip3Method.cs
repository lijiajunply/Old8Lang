using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.Zip3(second, third) - 将三个数组合并为三元组列表
/// </summary>
public class ArrayZip3Method : BaseInstanceMethod
{
    public override string[] Names => ["Zip3", "zip3"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[] ParameterNames => ["second", "third"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var secondValue = parameters[0].Run(manager);
        var thirdValue = parameters[1].Run(manager);

        if (secondValue is not ArrayLangValue secondArray)
        {
            throw new ArgumentError(position, "second 参数必须是数组类型");
        }

        if (thirdValue is not ArrayLangValue thirdArray)
        {
            throw new ArgumentError(position, "third 参数必须是数组类型");
        }

        var result = new List<LangValueType>();
        var minLength = Math.Min(Math.Min(array.RunResult.Length, secondArray.RunResult.Length), thirdArray.RunResult.Length);

        for (int i = 0; i < minLength; i++)
        {
            var tuple = CreateTupleWithValues(array.RunResult[i], secondArray.RunResult[i], thirdArray.RunResult[i]);
            result.Add(tuple);
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Array.Zip3 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not object?[] array)
        {
            throw new ArgumentException("实例必须是 object?[] 类型");
        }

        if (arguments.Length < 2)
        {
            throw new ArgumentException("需要两个参数");
        }

        if (arguments[0] is not object?[] second)
        {
            throw new ArgumentException("second 参数必须是数组类型");
        }

        if (arguments[1] is not object?[] third)
        {
            throw new ArgumentException("third 参数必须是数组类型");
        }

        var result = new List<object?>();
        var minLength = Math.Min(Math.Min(array.Length, second.Length), third.Length);

        for (int i = 0; i < minLength; i++)
        {
            result.Add(new object?[] { array[i], second[i], third[i] });
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
