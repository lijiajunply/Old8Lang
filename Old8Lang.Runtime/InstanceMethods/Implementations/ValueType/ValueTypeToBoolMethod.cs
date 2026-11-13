using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToBool() - 转换为布尔类型
/// </summary>
public class ValueTypeToBoolMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToBool", "toBool"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(BoolLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return ConvertToBool(instance);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ValueTypeToBoolMethod).GetMethod(nameof(ConvertToBoolHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static BoolLangValue ConvertToBoolHelper(LangValueType type)
    {
        return ConvertToBool(type);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var langValue = ConvertToLangValueType(instance);
        var result = ConvertToBool(langValue);
        return result.Value;
    }

    private static BoolLangValue ConvertToBool(LangValueType type)
    {
        if (type is BoolLangValue boolValue)
        {
            return boolValue;
        }

        if (type is IntLangValue intValue)
        {
            return new BoolLangValue(intValue.Value != 0);
        }

        if (type is DoubleLangValue doubleValue)
        {
            return new BoolLangValue(doubleValue.Value != 0.0);
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
                return new BoolLangValue(true);
            }
            if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return new BoolLangValue();
            }

            // Handle numeric strings
            if (int.TryParse(str, out var intResult))
            {
                return new BoolLangValue(intResult != 0);
            }

            if (double.TryParse(str, out var doubleResult))
            {
                return new BoolLangValue(doubleResult != 0.0);
            }

            // Non-empty string is true
            return new BoolLangValue(!string.IsNullOrEmpty(str));
        }

        if (type is NullLangValue)
        {
            return new BoolLangValue();
        }

        throw new FormatException($"Cannot convert {type.GetType().Name} to bool");
    }
}
