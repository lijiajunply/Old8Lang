using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Char;

/// <summary>
/// Char.IsControl() - 判断字符是否为控制字符
/// </summary>
public class CharIsControlMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsControl", "isControl"];
    public override Type TargetType => typeof(CharLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var charValue = (CharLangValue)instance;
        return BoolLangValue.Create(char.IsControl(charValue.Value), position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(CharIsControlMethod).GetMethod(nameof(IsControlHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool IsControlHelper(char c)
    {
        return char.IsControl(c);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not char c)
        {
            throw new ArgumentException("实例必须是 Char 类型");
        }

        return char.IsControl(c);
    }
}
