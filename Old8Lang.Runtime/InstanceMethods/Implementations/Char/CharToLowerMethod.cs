using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Char;

/// <summary>
/// Char.ToLower() - 将字符转换为小写
/// </summary>
public class CharToLowerMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToLower", "toLower", "Lower", "lower"];
    public override Type TargetType => typeof(CharLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var charValue = (CharLangValue)instance;
        return CharLangValue.Create(char.ToLower(charValue.Value), position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(CharToLowerMethod).GetMethod(nameof(ToLowerHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static char ToLowerHelper(char c)
    {
        return char.ToLower(c);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(char);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not char c)
        {
            throw new ArgumentException("实例必须是 Char 类型");
        }

        return char.ToLower(c);
    }
}
