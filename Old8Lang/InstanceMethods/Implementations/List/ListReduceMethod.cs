using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Reduce 方法 - 使用归约函数将列表元素归约为单个值
/// </summary>
public class ListReduceMethod : BaseInstanceMethod
{
    public override string[] Names => ["Reduce", "reduce"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["reducer", "initialValue"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var reducerParam = parameters[0].Run(manager);
        var initialValue = parameters[1].Run(manager);

        if (reducerParam is not FuncLangValue reducer)
        {
            throw new Error.TypeError(position, $"Reduce 方法的第一个参数必须是函数类型，但实际是 {reducerParam.GetType().Name}");
        }

        var accumulator = initialValue;
        foreach (var item in list.Values)
        {
            // 创建临时变量管理器
            var tempManager = new VariateManager();

            // 执行归约函数
            accumulator = reducer.Run(tempManager, [accumulator, item]);
        }

        return accumulator;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载归约函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载初始值
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var reduceHelperMethod = typeof(ListReduceMethod).GetMethod(nameof(ReduceHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, reduceHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：归约列表元素
    /// </summary>
    public static LangValueType ReduceHelper(ListLangValue list, LangValueType reducerParam, LangValueType initialValue)
    {
        if (reducerParam is not FuncLangValue reducer)
        {
            throw new Exception("Reduce 方法的第一个参数必须是函数类型");
        }

        var accumulator = initialValue;
        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            accumulator = reducer.Run(tempManager, [accumulator, item]);
        }

        return accumulator;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式下不支持高阶函数
        throw new NotSupportedException("Reduce 方法在 VM 模式下不支持");
    }
}
