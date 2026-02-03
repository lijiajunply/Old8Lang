using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.LastOrDefault 方法 - 返回最后一个元素，如果列表为空则返回 null
/// </summary>
public class ListLastOrDefaultMethod : BaseInstanceMethod
{
    public override string[] Names => ["LastOrDefault", "lastOrDefault"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        return list.Values.Count > 0 ? list.Values[^1] : NullLangValue.Instance;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListLastOrDefaultMethod).GetMethod(nameof(LastOrDefaultHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取最后一个元素或默认值
    /// </summary>
    public static LangValueType LastOrDefaultHelper(ListLangValue list)
    {
        return list.Values.Count > 0 ? list.Values[^1] : NullLangValue.Instance;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            return list.Count > 0 ? list[^1] : null;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
