using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Aggregate 方法 - 对列表元素进行聚合操作（无初始值）
/// </summary>
public class ListAggregateMethod : BaseInstanceMethod
{
    public override string[] Names => ["Aggregate", "aggregate", "Fold", "fold"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["accumulator"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var accumulatorParam = parameters[0].Run(manager);

        if (accumulatorParam is not FuncLangValue accumulator)
        {
            throw new Error.TypeError(position, $"Aggregate 方法的参数必须是函数类型，但实际是 {accumulatorParam.GetType().Name}");
        }

        if (list.Values.Count == 0)
        {
            throw new InvalidOperationException("无法对空列表进行聚合操作");
        }

        var result = list.Values[0];

        for (int i = 1; i < list.Values.Count; i++)
        {
            var tempManager = new VariateManager();
            result = accumulator.Run(tempManager, [result, list.Values[i]]);
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

        // 调用辅助方法
        var helperMethod = typeof(ListAggregateMethod).GetMethod(nameof(AggregateHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：聚合操作
    /// </summary>
    public static LangValueType AggregateHelper(ListLangValue list, LangValueType accumulatorParam)
    {
        if (list.Values.Count == 0)
        {
            throw new InvalidOperationException("无法对空列表进行聚合操作");
        }

        if (accumulatorParam is not FuncLangValue accumulator)
        {
            throw new Exception("参数必须是函数类型");
        }

        var result = list.Values[0];

        for (int i = 1; i < list.Values.Count; i++)
        {
            var tempManager = new VariateManager();
            result = accumulator.Run(tempManager, [result, list.Values[i]]);
        }

        return result;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("无法对空列表进行聚合操作");
            }

            var accumulator = arguments[0] as Func<object?, object?, object?>;
            if (accumulator == null)
            {
                throw new ArgumentException("参数必须是累加器函数");
            }

            var result = list[0];

            for (int i = 1; i < list.Count; i++)
            {
                result = accumulator(result, list[i]);
            }

            return result;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
