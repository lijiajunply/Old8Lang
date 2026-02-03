using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.ToStr 方法 - 将列表转换为字符串表示
/// </summary>
public class ListToStrMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToStr", "toStr", "ToString", "toString"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var items = list.Values.Select(item => item.ToString() ?? "null");
        return new StringLangValue("[" + string.Join(", ", items) + "]");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var toStrHelperMethod = typeof(ListToStrMethod).GetMethod(nameof(ToStrHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, toStrHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：转换为字符串
    /// </summary>
    public static StringLangValue ToStrHelper(ListLangValue list)
    {
        var items = list.Values.Select(item => item.ToString() ?? "null");
        return new StringLangValue("[" + string.Join(", ", items) + "]");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            var items = list.Select(item => item?.ToString() ?? "null");
            return "[" + string.Join(", ", items) + "]";
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
