using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

public class StringPadRightMethod : BaseInstanceMethod
{
    public override string[] Names => ["PadRight", "padRight"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[] ParameterNames => ["totalWidth", "paddingChar"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var widthParam = parameters[0].Run(manager);
        if (widthParam is not IntLangValue width)
            throw new TypeError(position, $"PadRight 方法的第一个参数必须是整数类型");

        if (parameters.Count > 1)
        {
            var charParam = parameters[1].Run(manager);
            if (charParam is StringLangValue charStr && charStr.Value.Length > 0)
                return new StringLangValue(str.Value.PadRight(width.Value, charStr.Value[0]));
            else if (charParam is CharLangValue charVal)
                return new StringLangValue(str.Value.PadRight(width.Value, charVal.Value));
            throw new TypeError(position, $"PadRight 方法的第二个参数必须是字符类型");
        }

        return new StringLangValue(str.Value.PadRight(width.Value));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        if (parameters.Count > 1)
        {
            parameters[1].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(StringPadRightMethod).GetMethod(nameof(PadRightWithCharHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(StringPadRightMethod).GetMethod(nameof(PadRightHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static StringLangValue PadRightHelper(StringLangValue str, IntLangValue width)
    {
        return new StringLangValue(str.Value.PadRight(width.Value));
    }

    public static StringLangValue PadRightWithCharHelper(StringLangValue str, IntLangValue width, LangValueType charParam)
    {
        char paddingChar = ' ';
        if (charParam is StringLangValue charStr && charStr.Value.Length > 0)
            paddingChar = charStr.Value[0];
        else if (charParam is CharLangValue charVal)
            paddingChar = charVal.Value;
        return new StringLangValue(str.Value.PadRight(width.Value, paddingChar));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length > 0 && arguments[0] is int width)
        {
            if (arguments.Length > 1 && arguments[1] is char paddingChar)
                return str.PadRight(width, paddingChar);
            return str.PadRight(width);
        }
        throw new ArgumentException("参数类型错误");
    }
}
