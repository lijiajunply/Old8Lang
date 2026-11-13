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
/// String.IndexOf 方法 - 查找子字符串的索引
/// </summary>
public class StringIndexOfMethod : BaseInstanceMethod
{
    public override string[] Names => ["IndexOf", "indexOf"];
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
            throw new TypeError(position, $"IndexOf 方法的参数必须是字符串类型，但实际是 {valueParam.GetType().Name}");
        }

        return new IntLangValue(str.Value.IndexOf(value.Value, StringComparison.Ordinal));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(StringIndexOfMethod).GetMethod(nameof(IndexOfHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static IntLangValue IndexOfHelper(StringLangValue str, StringLangValue value)
    {
        return new IntLangValue(str.Value.IndexOf(value.Value, StringComparison.Ordinal));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(IntLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length > 0 && arguments[0] is string value)
        {
            return str.IndexOf(value, StringComparison.Ordinal);
        }
        throw new ArgumentException("实例和参数必须是 string 类型");
    }
}
