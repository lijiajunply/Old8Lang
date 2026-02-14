using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToInt() - 转换为整数类型
/// </summary>
public class ValueTypeToIntMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToInt", "toInt"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(IntLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return ConvertToInt(instance);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        // 获取实例类型
        var instanceType = instance.OutputType(local);

        // 根据类型选择合适的辅助方法
        if (instanceType == typeof(int))
        {
            var helperMethod = typeof(ValueTypeToIntMethod).GetMethod(nameof(ConvertIntToIntLangValue),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else if (instanceType == typeof(long))
        {
            var helperMethod = typeof(ValueTypeToIntMethod).GetMethod(nameof(ConvertLongToIntLangValue),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else if (instanceType == typeof(double))
        {
            var helperMethod = typeof(ValueTypeToIntMethod).GetMethod(nameof(ConvertDoubleToIntLangValue),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else if (instanceType == typeof(bool))
        {
            var helperMethod = typeof(ValueTypeToIntMethod).GetMethod(nameof(ConvertBoolToIntLangValue),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else if (instanceType == typeof(char))
        {
            var helperMethod = typeof(ValueTypeToIntMethod).GetMethod(nameof(ConvertCharToIntLangValue),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else if (instanceType == typeof(string))
        {
            var helperMethod = typeof(ValueTypeToIntMethod).GetMethod(nameof(ConvertStringToInt),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            // LangValueType -> IntLangValue
            var helperMethod = typeof(ValueTypeToIntMethod).GetMethod(nameof(ConvertToIntHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static IntLangValue ConvertIntToIntLangValue(int value)
    {
        return new IntLangValue(value);
    }

    public static IntLangValue ConvertLongToIntLangValue(long value)
    {
        return new IntLangValue((int)value);
    }

    public static IntLangValue ConvertDoubleToIntLangValue(double value)
    {
        return new IntLangValue(Convert.ToInt32(value));
    }

    public static IntLangValue ConvertBoolToIntLangValue(bool value)
    {
        return new IntLangValue(value ? 1 : 0);
    }

    public static IntLangValue ConvertCharToIntLangValue(char value)
    {
        return new IntLangValue(Convert.ToInt32(value));
    }

    public static IntLangValue ConvertStringToInt(string str)
    {
        // Handle quoted strings
        if (str.StartsWith("\"") && str.EndsWith("\""))
        {
            str = str.Substring(1, str.Length - 2);
        }

        // Handle common boolean strings
        if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return new IntLangValue(1);
        }
        if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return new IntLangValue();
        }

        // Try to parse as integer
        if (int.TryParse(str, out var intResult))
        {
            return new IntLangValue(intResult);
        }

        // Try to parse as double and convert to int
        if (double.TryParse(str, out var doubleResult))
        {
            return new IntLangValue(Convert.ToInt32(doubleResult));
        }

        throw new FormatException($"Cannot convert '{str}' to integer");
    }

    public static IntLangValue ConvertToIntHelper(LangValueType type)
    {
        return ConvertToInt(type);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(IntLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var langValue = ConvertToLangValueType(instance);
        var result = ConvertToInt(langValue);
        return (long)result.Value;
    }

    private static IntLangValue ConvertToInt(LangValueType type)
    {
        if (type is IntLangValue intValue)
        {
            return intValue;
        }

        if (type is DoubleLangValue doubleValue)
        {
            return new IntLangValue(Convert.ToInt32(doubleValue.Value));
        }

        if (type is CharLangValue charValue)
        {
            return new IntLangValue(Convert.ToInt32(charValue.Value));
        }

        if (type is BoolLangValue boolValue)
        {
            return new IntLangValue(boolValue.Value ? 1 : 0);
        }

        if (type is StringLangValue stringValue)
        {
            var str = stringValue.Value;
            // Handle quoted strings
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                str = str.Substring(1, str.Length - 2);
            }

            // Handle common boolean strings
            if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return new IntLangValue(1);
            }
            if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return new IntLangValue();
            }

            // Try to parse as integer
            if (int.TryParse(str, out var intResult))
            {
                return new IntLangValue(intResult);
            }

            // Try to parse as double and convert to int
            if (double.TryParse(str, out var doubleResult))
            {
                return new IntLangValue(Convert.ToInt32(doubleResult));
            }

            throw new FormatException($"Cannot convert '{stringValue.Value}' to integer");
        }

        if (type is NullLangValue)
        {
            return new IntLangValue();
        }

        throw new FormatException($"Cannot convert {type.GetType().Name} to integer");
    }
}
