using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToChar() - 转换为字符类型
/// </summary>
public class ValueTypeToCharMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToChar", "toChar"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(CharLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return ConvertToChar(instance);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ValueTypeToCharMethod).GetMethod(nameof(ConvertToCharHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static CharLangValue ConvertToCharHelper(LangValueType type)
    {
        return ConvertToChar(type);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(CharLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var langValue = ConvertToLangValueType(instance);
        var result = ConvertToChar(langValue);
        return result.Value;
    }

    private static CharLangValue ConvertToChar(LangValueType type)
    {
        if (type is CharLangValue charValue)
        {
            return charValue;
        }

        if (type is IntLangValue intValue)
        {
            if (intValue.Value is >= 0 and <= 65535)
            {
                return new CharLangValue(Convert.ToChar(intValue.Value));
            }
            throw new FormatException($"Integer value {intValue.Value} is out of valid character range");
        }

        if (type is StringLangValue stringValue)
        {
            var str = stringValue.Value;
            // Handle quoted strings
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                str = str.Substring(1, str.Length - 2);
            }

            if (str.Length == 1)
            {
                return new CharLangValue(str[0]);
            }
            throw new FormatException($"String '{stringValue.Value}' is not a single character");
        }

        if (type is NullLangValue)
        {
            return new CharLangValue();
        }

        throw new FormatException($"Cannot convert {type.GetType().Name} to char");
    }
}
