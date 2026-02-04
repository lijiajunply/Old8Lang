using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

/// <summary>
/// String.Contains 方法 - 检查是否包含子字符串
/// </summary>
public class StringContainsMethod : BaseInstanceMethod
{
    public override string[] Names => ["Contains", "contains"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[] ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var valueParam = parameters[0].Run(manager);

        if (valueParam is not StringLangValue value)
        {
            throw new TypeError(position, $"Contains 方法的参数必须是字符串类型，但实际是 {valueParam.GetType().Name}");
        }

        return new BoolLangValue(str.Value.Contains(value.Value));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载字符串实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载要查找的值
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(StringContainsMethod).GetMethod(nameof(ContainsHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：检查是否包含子字符串
    /// </summary>
    public static BoolLangValue ContainsHelper(StringLangValue str, StringLangValue value)
    {
        return new BoolLangValue(str.Value.Contains(value.Value));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length > 0 && arguments[0] is string value)
        {
            return str.Contains(value);
        }

        throw new ArgumentException("实例和参数必须是 string 类型");
    }
}
