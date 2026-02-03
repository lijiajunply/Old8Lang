using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

public class StringReverseMethod : BaseInstanceMethod
{
    public override string[] Names => ["Reverse", "reverse"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var charArray = str.Value.ToCharArray();
        Array.Reverse(charArray);
        return new StringLangValue(new string(charArray));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(StringReverseMethod).GetMethod(nameof(ReverseHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static StringLangValue ReverseHelper(StringLangValue str)
    {
        var charArray = str.Value.ToCharArray();
        Array.Reverse(charArray);
        return new StringLangValue(new string(charArray));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str)
        {
            var charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        throw new ArgumentException("实例必须是 string 类型");
    }
}
