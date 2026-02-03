using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Map 方法 - 使用转换函数映射列表元素
/// </summary>
public class ListMapMethod : BaseInstanceMethod
{
    public override string[] Names => ["Map", "map"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["transform"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var transformParam = parameters[0].Run(manager);

        if (transformParam is not FuncLangValue transform)
        {
            throw new Error.TypeError(position, $"Map 方法的参数必须是函数类型，但实际是 {transformParam.GetType().Name}");
        }

        var mapped = new List<LangValueType>();
        foreach (var item in list.Values)
        {
            // 创建临时变量管理器
            var tempManager = new VariateManager();

            // 执行转换函数
            var result = transform.Run(tempManager, [item]);
            mapped.Add(result);
        }

        return new ListLangValue(mapped);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载转换函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var mapHelperMethod = typeof(ListMapMethod).GetMethod(nameof(MapHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, mapHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：映射列表元素
    /// </summary>
    public static ListLangValue MapHelper(ListLangValue list, LangValueType transformParam)
    {
        if (transformParam is not FuncLangValue transform)
        {
            throw new Exception("Map 方法的参数必须是函数类型");
        }

        var mapped = new List<LangValueType>();
        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var result = transform.Run(tempManager, [item]);
            mapped.Add(result);
        }

        return new ListLangValue(mapped);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式下不支持高阶函数
        throw new NotSupportedException("Map 方法在 VM 模式下不支持");
    }
}
