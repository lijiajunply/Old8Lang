using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToDouble() - 转换为浮点数类型
/// </summary>
public class ValueTypeToDoubleMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToDouble", "toDouble"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(DoubleLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return ConvertToDouble(instance);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ValueTypeToDoubleMethod).GetMethod(nameof(ConvertToDoubleHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static DoubleLangValue ConvertToDoubleHelper(LangValueType type)
    {
        return ConvertToDouble(type);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(DoubleLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var langValue = ConvertToLangValueType(instance);
        var result = ConvertToDouble(langValue);
        return result.Value;
    }

    private static DoubleLangValue ConvertToDouble(LangValueType type)
    {
        if (type is DoubleLangValue doubleValue)
        {
            return doubleValue;
        }

        if (type is IntLangValue intValue)
        {
            return new DoubleLangValue(Convert.ToDouble(intValue.Value));
        }

        if (type is StringLangValue stringValue)
        {
            var str = stringValue.Value;
            // Handle quoted strings
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                str = str.Substring(1, str.Length - 2);
            }

            // Handle boolean strings
            if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return new DoubleLangValue(1.0);
            }
            if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return new DoubleLangValue();
            }

            // Try to parse as double
            if (double.TryParse(str, out var doubleResult))
            {
                return new DoubleLangValue(doubleResult);
            }

            throw new FormatException($"Cannot convert '{stringValue.Value}' to double");
        }

        if (type is BoolLangValue boolValue)
        {
            return new DoubleLangValue(boolValue.Value ? 1.0 : 0.0);
        }

        if (type is NullLangValue)
        {
            return new DoubleLangValue();
        }

        throw new FormatException($"Cannot convert {type.GetType().Name} to double");
    }
}
