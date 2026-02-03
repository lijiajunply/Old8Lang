using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Last 方法 - 获取列表的最后一个元素
/// </summary>
public class ListLastMethod : BaseInstanceMethod
{
    public override string[] Names => ["Last", "last"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (list.Values.Count == 0)
        {
            throw new Error.InvalidOperationError(position, "无法获取空列表的最后一个元素");
        }

        return list.Values[^1];
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var lastHelperMethod = typeof(ListLastMethod).GetMethod(nameof(LastHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, lastHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取最后一个元素
    /// </summary>
    public static LangValueType LastHelper(ListLangValue list)
    {
        if (list.Values.Count == 0)
        {
            throw new Exception("无法获取空列表的最后一个元素");
        }

        return list.Values[^1];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("无法获取空列表的最后一个元素");
            }

            return list[^1];
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
