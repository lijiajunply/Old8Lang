using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.String;

public class StringPadLeftMethod : BaseInstanceMethod
{
    public override string[] Names => ["PadLeft", "padLeft"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[] ParameterNames => ["totalWidth", "paddingChar"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var widthParam = parameters[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
        if (widthParam is not IntLangValue width)
            throw new TypeError(position, $"PadLeft 方法的第一个参数必须是整数类型");

        if (parameters.Count > 1)
        {
            var charParam = parameters[1].Accept(RuntimeVisitors.CreateInterpreter(manager));
            if (charParam is StringLangValue charStr && charStr.Value.Length > 0)
                return new StringLangValue(str.Value.PadLeft(width.Value, charStr.Value[0]));
            else if (charParam is CharLangValue charVal)
                return new StringLangValue(str.Value.PadLeft(width.Value, charVal.Value));
            throw new TypeError(position, $"PadLeft 方法的第二个参数必须是字符类型");
        }

        return new StringLangValue(str.Value.PadLeft(width.Value));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
        if (parameters.Count > 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[1], ilGenerator, local);
            var helperMethod = typeof(StringPadLeftMethod).GetMethod(nameof(PadLeftWithCharHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(StringPadLeftMethod).GetMethod(nameof(PadLeftHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static string PadLeftHelper(StringLangValue str, IntLangValue width)
    {
        return str.Value.PadLeft(width.Value);
    }

    public static string PadLeftWithCharHelper(StringLangValue str, IntLangValue width, LangValueType charParam)
    {
        char paddingChar = ' ';
        if (charParam is StringLangValue charStr && charStr.Value.Length > 0)
            paddingChar = charStr.Value[0];
        else if (charParam is CharLangValue charVal)
            paddingChar = charVal.Value;
        return str.Value.PadLeft(width.Value, paddingChar);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length > 0 && arguments[0] is int width)
        {
            if (arguments.Length > 1 && arguments[1] is char paddingChar)
                return str.PadLeft(width, paddingChar);
            return str.PadLeft(width);
        }
        throw new ArgumentException("参数类型错误");
    }
}
