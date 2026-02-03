using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.AggregateWithSeed 方法 - 对列表元素进行聚合操作（带初始值）
/// </summary>
public class ListAggregateWithSeedMethod : BaseInstanceMethod
{
    public override string[] Names => ["AggregateWith", "aggregateWith", "FoldWith", "foldWith"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["accumulator", "seed"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var accumulatorParam = parameters[0].Run(manager);
        var seed = parameters[1].Run(manager);

        if (accumulatorParam is not FuncLangValue accumulator)
        {
            throw new Error.TypeError(position, $"AggregateWith 方法的第一个参数必须是函数类型，但实际是 {accumulatorParam.GetType().Name}");
        }

        var result = seed;

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            result = accumulator.Run(tempManager, [result, item]);
        }

        return result;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载累加器函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载初始值
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListAggregateWithSeedMethod).GetMethod(nameof(AggregateWithSeedHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：带初始值的聚合操作
    /// </summary>
    public static LangValueType AggregateWithSeedHelper(ListLangValue list, LangValueType accumulatorParam, LangValueType seed)
    {
        if (accumulatorParam is not FuncLangValue accumulator)
        {
            throw new Exception("第一个参数必须是函数类型");
        }

        var result = seed;

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            result = accumulator.Run(tempManager, [result, item]);
        }

        return result;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length >= 2)
        {
            var accumulator = arguments[0] as Func<object?, object?, object?>;
            if (accumulator == null)
            {
                throw new ArgumentException("第一个参数必须是累加器函数");
            }

            var result = arguments[1];

            foreach (var item in list)
            {
                result = accumulator(result, item);
            }

            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
