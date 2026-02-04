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
/// Array.Set(index, value) - 设置数组元素
/// </summary>
public class ArraySetMethod : BaseInstanceMethod
{
    public override string[] Names => ["Set", "set"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => ["index", "value"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var indexValue = parameters[0].Run(manager);
        var value = parameters[1].Run(manager);

        if (indexValue is not IntLangValue intIndex)
        {
            throw new TypeError(position, "IntValue", indexValue.GetType().Name);
        }

        array.Set(intIndex, value);
        return value;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        parameters[1].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ArraySetMethod).GetMethod(nameof(SetHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object? SetHelper(object[] array, object indexObj, object? value)
    {
        if (indexObj is not int index)
        {
            throw new ArgumentException("索引必须是整数类型");
        }

        // 支持负数索引
        if (index < 0)
        {
            index = array.Length + index;
        }

        if (index < 0 || index >= array.Length)
        {
            throw new IndexOutOfRangeException($"索引 {index} 超出数组范围 [0, {array.Length})");
        }

        array[index] = value;
        return value;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not object[] array)
        {
            throw new ArgumentException("实例必须是 object[] 类型");
        }

        if (arguments[0] is not int index)
        {
            throw new ArgumentException("索引必须是整数类型");
        }

        // 支持负数索引
        if (index < 0)
        {
            index = array.Length + index;
        }

        if (index < 0 || index >= array.Length)
        {
            throw new IndexOutOfRangeException($"索引 {index} 超出数组范围 [0, {array.Length})");
        }

        array[index] = arguments[1];
        return arguments[1];
    }
}
