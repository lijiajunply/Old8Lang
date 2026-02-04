using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Take 方法 - 获取列表的前n个元素
/// </summary>
public class ListTakeMethod : BaseInstanceMethod
{
    public override string[] Names => ["Take", "take"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["count"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var countParam = parameters[0].Run(manager);

        if (countParam is not IntLangValue countValue)
        {
            throw new Error.TypeError(position, $"Take 方法的参数必须是整数类型，但实际是 {countParam.GetType().Name}");
        }

        var takeCount = Math.Max(0, Math.Min(countValue.Value, list.Values.Count));
        var result = list.Values.Take(takeCount).ToList();
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载获取数量
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var takeHelperMethod = typeof(ListTakeMethod).GetMethod(nameof(TakeHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, takeHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取前n个元素
    /// </summary>
    public static ListLangValue TakeHelper(ListLangValue list, LangValueType countParam)
    {
        if (countParam is not IntLangValue countValue)
        {
            throw new Exception("Take 方法的参数必须是整数类型");
        }

        var takeCount = Math.Max(0, Math.Min(countValue.Value, list.Values.Count));
        var result = list.Values.Take(takeCount).ToList();
        return new ListLangValue(result);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (arguments[0] is not int count)
            {
                throw new ArgumentException("Take 方法的参数必须是整数类型");
            }

            var takeCount = Math.Max(0, Math.Min(count, list.Count));
            return list.Take(takeCount).ToList();
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
