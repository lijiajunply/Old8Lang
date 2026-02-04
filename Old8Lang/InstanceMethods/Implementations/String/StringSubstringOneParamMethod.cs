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
/// String.Substring(start) - 从指定位置开始获取子串到末尾（单参数版本）
/// </summary>
public class StringSubstringOneParamMethod : BaseInstanceMethod
{
    public override string[] Names => ["Substring", "substring"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => ["start"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var startParam = parameters[0].Run(manager);

        if (startParam is not IntLangValue start)
        {
            throw new TypeError(position, "int", startParam.TypeToString());
        }

        return new StringLangValue(str.Value.Substring(start.Value));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(StringSubstringOneParamMethod).GetMethod(nameof(SubstringHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static StringLangValue SubstringHelper(StringLangValue str, IntLangValue start)
    {
        return new StringLangValue(str.Value.Substring(start.Value));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments[0] is int start)
        {
            return str.Substring(start);
        }

        throw new ArgumentException("参数类型不匹配");
    }
}
