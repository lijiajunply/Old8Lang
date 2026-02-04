using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Reverse 方法 - 反转列表元素顺序
/// </summary>
public class ListReverseMethod : BaseInstanceMethod
{
    public override string[] Names => ["Reverse", "reverse"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        // 创建新列表副本以避免修改原列表
        var reversedValues = new List<LangValueType>(list.Values);
        reversedValues.Reverse();
        return new ListLangValue(reversedValues);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var reverseHelperMethod = typeof(ListReverseMethod).GetMethod(nameof(ReverseHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, reverseHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：反转列表
    /// </summary>
    public static ListLangValue ReverseHelper(ListLangValue list)
    {
        var reversedValues = new List<LangValueType>(list.Values);
        reversedValues.Reverse();
        return new ListLangValue(reversedValues);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            var reversed = new List<object?>(list);
            reversed.Reverse();
            return reversed;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
