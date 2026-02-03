using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.ForEach 方法 - 对列表中的每个元素执行操作
/// </summary>
public class ListForEachMethod : BaseInstanceMethod
{
    public override string[] Names => ["ForEach", "forEach", "Each", "each"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => ["action"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var actionParam = parameters[0].Run(manager);

        if (actionParam is not FuncLangValue action)
        {
            throw new Error.TypeError(position, $"ForEach 方法的参数必须是函数类型，但实际是 {actionParam.GetType().Name}");
        }

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            action.Run(tempManager, [item]);
        }

        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载操作函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListForEachMethod).GetMethod(nameof(ForEachHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：对每个元素执行操作
    /// </summary>
    public static VoidLangValue ForEachHelper(ListLangValue list, LangValueType actionParam)
    {
        if (actionParam is not FuncLangValue action)
        {
            throw new Exception("参数必须是函数类型");
        }

        foreach (var item in list.Values)
        {
            var tempManager = new VariateManager();
            action.Run(tempManager, [item]);
        }

        return new VoidLangValue();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(VoidLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            var action = arguments[0] as Action<object?>;
            if (action == null)
            {
                throw new ArgumentException("参数必须是操作函数");
            }

            foreach (var item in list)
            {
                action(item);
            }

            return null;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
