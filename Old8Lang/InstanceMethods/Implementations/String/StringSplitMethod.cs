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
/// String.Split 方法 - 分割字符串
/// </summary>
public class StringSplitMethod : BaseInstanceMethod
{
    public override string[] Names => ["Split", "split"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => ["separator"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var separatorValue = parameters[0].Run(manager);

        if (separatorValue is not StringLangValue separator)
        {
            throw new TypeError(position, $"Split 方法的参数必须是字符串类型，但实际是 {separatorValue.GetType().Name}");
        }

        var parts = str.Value.Split(new[] { separator.Value }, StringSplitOptions.None);
        var result = parts.Select(p => (LangValueType)new StringLangValue(p)).ToList();

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载字符串实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载分隔符
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(StringSplitMethod).GetMethod(nameof(SplitHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：分割字符串
    /// </summary>
    public static ListLangValue SplitHelper(StringLangValue str, StringLangValue separator)
    {
        var parts = str.Value.Split(new[] { separator.Value }, StringSplitOptions.None);
        var result = parts.Select(p => (LangValueType)new StringLangValue(p)).ToList();
        return new ListLangValue(result);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length > 0 && arguments[0] is string separator)
        {
            return str.Split(new[] { separator }, StringSplitOptions.None).ToList();
        }

        throw new ArgumentException("实例和参数必须是 string 类型");
    }
}
