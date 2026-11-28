using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;

using Old8Lang.AST.Visitor;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Len 函数 - 获取列表、数组、字符串等的长度
/// </summary>
public sealed class LenFunction : BaseGlobalFunction
{
    public override string[] Names => ["Len", "len"];
    public override string[] ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var value = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        if (value is ILangList list)
        {
            return new IntLangValue(list.GetLength());
        }
        throw new InvalidOperationError(position, $"{value} 不是列表类型");
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        var lenId = parameters[0];
        lenId.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var lenType = lenId.Accept(new TypeInferenceVisitor(local))!;

        // 尝试获取Length属性，适用于数组、字符串等
        var lengthProp = lenType.GetProperty("Length");
        if (lengthProp is not null)
        {
            ilGenerator.Emit(OpCodes.Call, lengthProp.GetGetMethod()!);
            return;
        }

        // 尝试获取Count属性，适用于集合类
        var countProp = lenType.GetProperty("Count");
        if (countProp is not null)
        {
            ilGenerator.Emit(OpCodes.Call, countProp.GetGetMethod()!);
            return;
        }

        // 尝试获取Length字段
        var lengthField = lenType.GetField("Length");
        if (lengthField is not null)
        {
            ilGenerator.Emit(OpCodes.Ldfld, lengthField);
            return;
        }

        // 尝试获取Count字段
        var countField = lenType.GetField("Count");
        if (countField is not null)
        {
            ilGenerator.Emit(OpCodes.Ldfld, countField);
            return;
        }

        // 静态类型为 object（未加注解的局部变量、集合索引结果等）时无法在编译期确定
        // Length/Count 成员，改为运行期按实际值求长度。这里不能再压 Ldc_I4_0：
        // 参数值已经压栈，直接返回会留下悬空栈值，生成无效 IL。
        if (lenType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Call, typeof(LenFunction).GetMethod(nameof(GetLengthDynamic))!);
            return;
        }

        throw new InvalidOperationError(position, $"类型 {lenType.Name} 没有 Length 或 Count 属性");
    }

    /// <summary>
    /// 运行期求长度，供静态类型为 <see cref="object"/> 的 <c>Len</c> 调用。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="ExecuteInternal"/> 的解释器口径一致：语言列表 / 字符串 / 集合都按元素数，
    /// 其它类型报错。
    /// </remarks>
    public static int GetLengthDynamic(object? value)
    {
        return value switch
        {
            null => throw new InvalidOperationError(new SourcePosition(0, 0), "Null 不是列表类型"),
            string text => text.Length,
            ILangList list => list.GetLength(),
            System.Collections.ICollection collection => collection.Count,
            _ => throw new InvalidOperationError(new SourcePosition(0, 0), $"{value} 不是列表类型")
        };
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        // VM 模式下需要获取参数的长度
        object? value = arguments[0];
        if (value is Array array)
        {
            return array.Length;
        }
        if (value is string str)
        {
            return str.Length;
        }
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count;
        }
        return 0;
    }
}

/// <summary>
/// Range 函数 - 创建整数序列 [start, end)，步长为 step
/// </summary>
public sealed class RangeFunction : BaseGlobalFunction
{
    public override string[] Names => ["Range", "range"];
    public override string[] ParameterNames => ["start", "end", "step"];
    public override Type?[] ParameterTypes => [typeof(int), typeof(int), typeof(int)];
    public override Type? DeclaredReturnType => typeof(int[]);
    public override string? Documentation => "创建一个整数范围序列";
    public override int MinParameterCount => 3;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var values = EvaluateParameters(parameters, manager);
        var start = ToInt(values[0], position);
        var end = ToInt(values[1], position);
        var step = ToInt(values[2], position);

        var result = BuildRangeValues(start, end, step, position);
        return new ArrayLangValue(result, "int", position);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 参数按原生值压栈后交给运行期构建（与 VM 的 ExecuteInVMInternal 同一实现）。
        // 之前这里直接返回空数组，IL 模式下 Range(...) 永远是 []，与 -f / -vm 不一致。
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            parameter.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var parameterType = parameter.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (parameterType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, parameterType);
            }
        }

        ilGenerator.Emit(OpCodes.Call, typeof(RangeFunction).GetMethod(nameof(BuildIlRange))!);
    }

    /// <summary>
    /// 运行期构建 Range 结果，供 IL 模式调用。
    /// </summary>
    public static int[] BuildIlRange(object? startValue, object? endValue, object? stepValue)
    {
        var start = Convert.ToInt32(startValue);
        var end = Convert.ToInt32(endValue);
        var step = Convert.ToInt32(stepValue);
        return BuildRangeIntegers(start, end, step, new SourcePosition()).ToArray();
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int[]);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var start = Convert.ToInt32(arguments[0]);
        var end = Convert.ToInt32(arguments[1]);
        var step = Convert.ToInt32(arguments[2]);

        var values = BuildRangeIntegers(start, end, step, new SourcePosition());
        return values.ToArray();
    }

    private static List<LangValueType> BuildRangeValues(int start, int end, int step, SourcePosition position)
    {
        var numbers = BuildRangeIntegers(start, end, step, position);
        return numbers.Select(static n => (LangValueType)new IntLangValue(n)).ToList();
    }

    private static List<int> BuildRangeIntegers(int start, int end, int step, SourcePosition position)
    {
        if (step == 0)
        {
            throw new InvalidOperationError(position, "Range 的 step 不能为 0");
        }

        var result = new List<int>();
        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Add(i);
            }
        }
        else
        {
            for (int i = start; i > end; i += step)
            {
                result.Add(i);
            }
        }

        return result;
    }

    private static int ToInt(LangValueType value, SourcePosition position)
    {
        return value switch
        {
            IntLangValue intValue => intValue.Value,
            DoubleLangValue doubleValue => (int)doubleValue.Value,
            StringLangValue stringValue when int.TryParse(stringValue.Value, out var n) => n,
            BoolLangValue boolValue => boolValue.Value ? 1 : 0,
            _ => throw new InvalidOperationError(position, $"Range 参数必须是可转换为 int 的值，实际为 {value.TypeToString()}")
        };
    }
}

/// <summary>
/// Type 函数 - 获取值的类型名称
/// </summary>
public sealed class TypeFunction : BaseGlobalFunction
{
    public override string[] Names => ["Type", "type"];
    public override string[] ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        return new TypeLangValue(parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager))).Accept(RuntimeVisitors.CreateInterpreter(manager));
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // IL 模式下 type() 与解释器/VM 统一返回语言类型名（Int / Double / String / List 等）。
        // 静态类型为 object 或未知时在运行期按实际值解析，避免返回无意义的 "Object"。
        var typeId = parameters[0];
        var typeIdType = typeId.Accept(new TypeInferenceVisitor(local));
        if (typeIdType is null || typeIdType == typeof(object))
        {
            typeId.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            ilGenerator.Emit(OpCodes.Call,
                typeof(VMReflectionHelper).GetMethod(nameof(VMReflectionHelper.DescribeValueType))!);
            return;
        }

        ilGenerator.Emit(OpCodes.Ldstr, VMReflectionHelper.DescribeTypeName(typeIdType));
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return VMReflectionHelper.DescribeValueType(arguments[0]);
    }
}

/// <summary>
/// Assert 函数 - 断言两个值相等
/// </summary>
public sealed class AssertFunction : BaseGlobalFunction
{
    public override string[] Names => ["Assert", "assert"];
    public override string[] ParameterNames => ["actual", "expected"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var value = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        var value1 = parameters[1].Accept(RuntimeVisitors.CreateInterpreter(manager));

        if (!value.Equal(value1))
        {
            var message = $"断言失败: 期望 {value1}，但得到 {value}";
            throw new AssertionError(position, message);
        }

        return new BoolLangValue(true);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // IL 模式暂不支持断言，返回 true
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        // VM 模式下暂不支持断言,返回 true
        return true;
    }
}

/// <summary>
/// ShowValues 函数 - 显示变量管理器的内容（调试用）
/// </summary>
public sealed class ShowValuesFunction : BaseGlobalFunction
{
    public override string[] Names => ["ShowValues", "showValues"];
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
#if DEBUG
        manager.Interpreter.OutputProvider.WriteLine(manager.ToString());
#endif
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // IL 模式不做任何操作
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        // VM 模式下不支持 ShowValues,返回 null
        return null;
    }
}
