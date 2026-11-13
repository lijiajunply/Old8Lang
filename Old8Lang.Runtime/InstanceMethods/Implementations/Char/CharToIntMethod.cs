using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Char;

/// <summary>
/// Char.ToInt() - 将字符转换为整数（ASCII 值）
/// </summary>
public class CharToIntMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToInt", "toInt"];
    public override Type TargetType => typeof(CharLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var charValue = (CharLangValue)instance;
        return IntLangValue.Create((int)charValue.Value, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(CharToIntMethod).GetMethod(nameof(ToIntHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static int ToIntHelper(char c)
    {
        return (int)c;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not char c)
        {
            throw new ArgumentException("实例必须是 Char 类型");
        }

        return (int)c;
    }
}
