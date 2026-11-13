using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.Equal() / EqualsTo() - 比较当前值与另一个值是否相等
/// </summary>
public class ValueTypeEqualMethod : BaseValueTypeMethod
{
    public override string[] Names => ["Equal", "equal", "EqualsTo", "equalsTo"];
    public override string[]? ParameterNames => ["otherValue"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;
    public override Type? DeclaredReturnType => typeof(BoolLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var otherValue = parameters[0].Run(manager);
        return new BoolLangValue(instance.Equal(otherValue));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ValueTypeEqualMethod).GetMethod(nameof(EqualHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool EqualHelper(LangValueType instance, LangValueType otherValue)
    {
        return instance.Equal(otherValue);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (arguments.Length != 1)
        {
            throw new ArgumentException("Equal 方法需要 1 个参数");
        }

        var langValue = ConvertToLangValueType(instance);
        var otherValue = ConvertToLangValueType(arguments[0]);
        return langValue.Equal(otherValue);
    }
}
