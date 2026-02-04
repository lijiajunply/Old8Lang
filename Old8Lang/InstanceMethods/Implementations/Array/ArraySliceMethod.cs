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
/// Array.Slice(start, end, step=1) - 数组切片
/// </summary>
public class ArraySliceMethod : BaseInstanceMethod
{
    public override string[] Names => ["Slice", "slice"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => ["start", "end", "step"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var startValue = parameters[0].Run(manager);
        var endValue = parameters[1].Run(manager);

        if (startValue is not IntLangValue startInt)
        {
            throw new TypeError(position, "IntValue", startValue.GetType().Name);
        }

        if (endValue is not IntLangValue endInt)
        {
            throw new TypeError(position, "IntValue", endValue.GetType().Name);
        }

        int step = 1;
        if (parameters.Count == 3)
        {
            var stepValue = parameters[2].Run(manager);
            if (stepValue is not IntLangValue stepInt)
            {
                throw new TypeError(position, "IntValue", stepValue.GetType().Name);
            }
            step = stepInt.Value;
        }

        return array.Slice(startInt.Value, endInt.Value, step);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        parameters[1].LoadIlValue(ilGenerator, local);

        if (parameters.Count == 3)
        {
            parameters[2].LoadIlValue(ilGenerator, local);
        }
        else
        {
            // 默认步长为 1
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
        }

        var helperMethod = typeof(ArraySliceMethod).GetMethod(nameof(SliceHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object[] SliceHelper(object[] array, object startObj, object endObj, object stepObj)
    {
        if (startObj is not int start)
        {
            throw new ArgumentException("起始索引必须是整数类型");
        }

        if (endObj is not int end)
        {
            throw new ArgumentException("结束索引必须是整数类型");
        }

        if (stepObj is not int step)
        {
            throw new ArgumentException("步长必须是整数类型");
        }

        var length = array.Length;
        var result = new List<object?>();

        if (step > 0)
        {
            // 正向切片
            if (start < 0) start += length;
            if (end < 0) end += length;

            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            for (int i = start; i < end; i += step)
            {
                result.Add(array[i]);
            }
        }
        else if (step < 0)
        {
            // 反向切片
            if (start < -1) start += length;
            if (end < -1) end += length;

            if (start >= length) start = length - 1;
            if (start < -1) start = -1;
            if (end >= length) end = length - 1;

            for (int i = start; i > end; i += step)
            {
                result.Add(array[i]);
            }
        }
        else
        {
            throw new ArgumentException("切片步长不能为0");
        }

        return result.ToArray();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object[]);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not object[] array)
        {
            throw new ArgumentException("实例必须是 object[] 类型");
        }

        if (arguments[0] is not int start)
        {
            throw new ArgumentException("起始索引必须是整数类型");
        }

        if (arguments[1] is not int end)
        {
            throw new ArgumentException("结束索引必须是整数类型");
        }

        int step = 1;
        if (arguments.Length >= 3 && arguments[2] is int stepValue)
        {
            step = stepValue;
        }

        return SliceHelper(array, start, end, step);
    }
}
