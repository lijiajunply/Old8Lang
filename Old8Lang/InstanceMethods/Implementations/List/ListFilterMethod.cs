using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Filter 方法 - 使用谓词函数过滤列表元素
/// </summary>
public class ListFilterMethod : BaseInstanceMethod
{
    public override string[] Names => ["Filter", "filter"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["predicate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var predicateParam = parameters[0].Run(manager);

        if (predicateParam is not FuncLangValue predicate)
        {
            throw new Error.TypeError(position, $"Filter 方法的参数必须是函数类型，但实际是 {predicateParam.GetType().Name}");
        }

        var filtered = new List<LangValueType>();
        foreach (var item in list.Values)
        {
            // 创建临时变量管理器
            var tempManager = new VariateManager();

            // 执行谓词函数
            var result = predicate.Run(tempManager, [item]);

            // 如果结果为真，则保留该元素
            if (result is BoolLangValue { Value: true })
            {
                filtered.Add(item);
            }
        }

        return new ListLangValue(filtered);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载谓词函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var filterHelperMethod = typeof(ListFilterMethod).GetMethod(nameof(FilterHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, filterHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：过滤列表元素
    /// </summary>
    public static ListLangValue FilterHelper(ListLangValue list, LangValueType predicateParam)
    {
        if (predicateParam is not FuncLangValue predicate)
        {
            throw new Exception("Filter 方法的参数必须是函数类型");
        }

        var filtered = new List<LangValueType>();
        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            var result = predicate.Run(tempManager, [item]);

            if (result is BoolLangValue { Value: true })
            {
                filtered.Add(item);
            }
        }

        return new ListLangValue(filtered);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式下不支持高阶函数
        throw new NotSupportedException("Filter 方法在 VM 模式下不支持");
    }
}
