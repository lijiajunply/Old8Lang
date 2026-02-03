using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

public class StringToCharArrayMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToCharArray", "toCharArray", "ToChars", "toChars"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var chars = str.Value.Select(c => (LangValueType)new CharLangValue(c)).ToList();
        return new ListLangValue(chars);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(StringToCharArrayMethod).GetMethod(nameof(ToCharArrayHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue ToCharArrayHelper(StringLangValue str)
    {
        var chars = str.Value.Select(c => (LangValueType)new CharLangValue(c)).ToList();
        return new ListLangValue(chars);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str)
        {
            return str.ToCharArray().Cast<object?>().ToList();
        }
        throw new ArgumentException("实例必须是 string 类型");
    }
}
