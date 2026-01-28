using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Int 函数 - 将值转换为整数
/// </summary>
public sealed class IntFunction : BaseGlobalFunction
{
    public override string[] Names => ["int", "Int"];
    public override string[]? ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager,
        SourcePosition position)
    {
        var value = parameters[0].Run(manager);

        if (value is IntLangValue intValue)
        {
            return intValue;
        }

        if (value is DoubleLangValue doubleValue)
        {
            return new IntLangValue((int)doubleValue.Value);
        }

        if (value is StringLangValue stringValue)
        {
            if (int.TryParse(stringValue.Value, out int result))
            {
                return new IntLangValue(result);
            }

            throw new CastError(value, "string", "int", $"字符串 '{stringValue.Value}' 不是有效的整数格式");
        }

        if (value is BoolLangValue boolValue)
        {
            return new IntLangValue(boolValue.Value ? 1 : 0);
        }

        throw new CastError(position, value.GetType().Name, "int");
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator,
        LocalManager local, SourcePosition position)
    {
        var param = parameters[0];
        param.LoadIlValue(ilGenerator, local);
        var paramType = param.OutputType(local)!;

        // 如果已经是 int 类型，直接返回
        if (paramType == typeof(int))
        {
            return;
        }

        // double -> int
        if (paramType == typeof(double))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
            return;
        }

        // bool -> int
        if (paramType == typeof(bool))
        {
            // bool 在 IL 中已经是 int32 (0 或 1)，不需要转换
            return;
        }

        // string -> int
        if (paramType == typeof(string))
        {
            var parseMethod = typeof(int).GetMethod("Parse", [typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, parseMethod);
            return;
        }

        // object -> int (运行时转换)
        if (paramType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(object)])!);
            return;
        }

        throw new CastError(position, paramType.Name, "int");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        object? value = arguments[0];

        if (value is int intValue)
        {
            return intValue;
        }

        if (value is double doubleValue)
        {
            return (int)doubleValue;
        }

        if (value is string stringValue)
        {
            if (int.TryParse(stringValue, out int result))
            {
                return result;
            }

            throw new CastError(new SourcePosition(), "string", "int", $"字符串 '{stringValue}' 不是有效的整数格式");
        }

        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        // 尝试使用 Convert.ToInt32
        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            throw new CastError(new SourcePosition(), value?.GetType().Name ?? "null", "int");
        }
    }
}

/// <summary>
/// Double 函数 - 将值转换为浮点数
/// </summary>
public sealed class DoubleFunction : BaseGlobalFunction
{
    public override string[] Names => ["double", "Double"];
    public override string[]? ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager,
        SourcePosition position)
    {
        var value = parameters[0].Run(manager);

        if (value is DoubleLangValue doubleValue)
        {
            return doubleValue;
        }

        if (value is IntLangValue intValue)
        {
            return new DoubleLangValue(intValue.Value);
        }

        if (value is StringLangValue stringValue)
        {
            if (double.TryParse(stringValue.Value, out double result))
            {
                return new DoubleLangValue(result);
            }

            throw new CastError(position, "string", "double", $"字符串 '{stringValue.Value}' 不是有效的浮点数格式");
        }

        if (value is BoolLangValue boolValue)
        {
            return new DoubleLangValue(boolValue.Value ? 1.0 : 0.0);
        }

        throw new CastError(position, value.GetType().Name, "double");
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator,
        LocalManager local, SourcePosition position)
    {
        var param = parameters[0];
        param.LoadIlValue(ilGenerator, local);
        var paramType = param.OutputType(local)!;

        // 如果已经是 double 类型，直接返回
        if (paramType == typeof(double))
        {
            return;
        }

        // int -> double
        if (paramType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Conv_R8);
            return;
        }

        // bool -> double
        if (paramType == typeof(bool))
        {
            // bool -> int -> double
            ilGenerator.Emit(OpCodes.Conv_R8);
            return;
        }

        // string -> double
        if (paramType == typeof(string))
        {
            var parseMethod = typeof(double).GetMethod("Parse", [typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, parseMethod);
            return;
        }

        // object -> double (运行时转换)
        if (paramType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToDouble", [typeof(object)])!);
            return;
        }

        throw new CastError(position, paramType.Name, "double");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(double);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        object? value = arguments[0];

        if (value is double doubleValue)
        {
            return doubleValue;
        }

        if (value is int intValue)
        {
            return (double)intValue;
        }

        if (value is string stringValue)
        {
            if (double.TryParse(stringValue, out double result))
            {
                return result;
            }

            throw new CastError(new SourcePosition(), "string", "double", $"字符串 '{stringValue}' 不是有效的浮点数格式");
        }

        if (value is bool boolValue)
        {
            return boolValue ? 1.0 : 0.0;
        }

        // 尝试使用 Convert.ToDouble
        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            throw new CastError(new SourcePosition(), value?.GetType().Name ?? "null", "double");
        }
    }
}

/// <summary>
/// Double 函数 - 将值转换为浮点数
/// </summary>
public sealed class CharFunction : BaseGlobalFunction
{
    public override string[] Names => ["char", "Char"];
    public override string[]? ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager,
        SourcePosition position)
    {
        var value = parameters[0].Run(manager);

        if (value is CharLangValue charValue)
        {
            return charValue;
        }

        if (value is IntLangValue intValue)
        {
            return new CharLangValue(Convert.ToChar(intValue.Value));
        }

        if (value is StringLangValue stringValue)
        {
            return stringValue.Value.Length == 1
                ? new CharLangValue(stringValue.Value[0])
                : new CharLangValue(char.Parse(stringValue.Value));
        }

        throw new CastError(position, value.GetType().Name, "char");
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator,
        LocalManager local, SourcePosition position)
    {
        var param = parameters[0];
        param.LoadIlValue(ilGenerator, local);
        var paramType = param.OutputType(local)!;

        // 如果已经是 char 类型，直接返回
        if (paramType == typeof(char))
        {
            return;
        }

        // string -> char
        if (paramType == typeof(string))
        {
            var parseMethod = typeof(char).GetMethod("Parse", [typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, parseMethod);
            return;
        }

        // object -> char 或者 int -> char
        if (paramType == typeof(object) || paramType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToChar", [typeof(object)])!);
            return;
        }

        throw new CastError(position, paramType.Name, "char");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(double);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        object? value = arguments[0];

        if (value is char charValue)
        {
            return charValue;
        }

        if (value is int intValue)
        {
            return Convert.ToChar(intValue);
        }

        if (value is string stringValue)
        {
            if (stringValue.Length == 1)
            {
                return stringValue[0];
            }
            throw new CastError(new SourcePosition(), "string", "char", $"字符串 '{stringValue}' 不是有效的字符格式（长度必须为1）");
        }

        // 尝试使用 Convert.ToChar
        try
        {
            return Convert.ToChar(value);
        }
        catch
        {
            throw new CastError(new SourcePosition(), value?.GetType().Name ?? "null", "char");
        }
    }
}

/// <summary>
/// Bool 函数 - 将值转换为布尔值
/// </summary>
public sealed class BoolFunction : BaseGlobalFunction
{
    public override string[] Names => ["bool", "Bool"];
    public override string[]? ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager,
        SourcePosition position)
    {
        var value = parameters[0].Run(manager);

        if (value is BoolLangValue boolValue)
        {
            return boolValue;
        }

        if (value is IntLangValue intValue)
        {
            return new BoolLangValue(intValue.Value != 0);
        }

        if (value is DoubleLangValue doubleValue)
        {
            return new BoolLangValue(doubleValue.Value != 0.0);
        }

        if (value is StringLangValue stringValue)
        {
            if (bool.TryParse(stringValue.Value, out bool result))
            {
                return new BoolLangValue(result);
            }
            // 字符串不是有效的布尔值，抛出异常
            throw new CastError(position, "string", "bool",
                $"字符串 '{stringValue.Value}' 不是有效的布尔值（只接受 'true' 或 'false'，不区分大小写）");
        }

        // null 为 false，其他对象为 true
        if (value is NullLangValue)
        {
            return new BoolLangValue(false);
        }

        return new BoolLangValue(true);
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator,
        LocalManager local, SourcePosition position)
    {
        var param = parameters[0];
        param.LoadIlValue(ilGenerator, local);
        var paramType = param.OutputType(local)!;

        // 如果已经是 bool 类型，直接返回
        if (paramType == typeof(bool))
        {
            return;
        }

        // int -> bool
        if (paramType == typeof(int))
        {
            // 比较是否不等于 0
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Cgt_Un);
            return;
        }

        // double -> bool
        if (paramType == typeof(double))
        {
            // 比较是否不等于 0.0
            ilGenerator.Emit(OpCodes.Ldc_R8, 0.0);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Ceq);
            return;
        }

        // string -> bool
        if (paramType == typeof(string))
        {
            var parseMethod = typeof(bool).GetMethod("Parse", [typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, parseMethod);
            return;
        }

        // object -> bool (运行时转换)
        if (paramType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToBoolean", [typeof(object)])!);
            return;
        }

        throw new CastError(position, paramType.Name, "bool");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        object? value = arguments[0];

        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (value is int intValue)
        {
            return intValue != 0;
        }

        if (value is double doubleValue)
        {
            return doubleValue != 0.0;
        }

        if (value is string stringValue)
        {
            if (bool.TryParse(stringValue, out bool result))
            {
                return result;
            }
            // 字符串不是有效的布尔值，抛出异常
            throw new CastError(new SourcePosition(), "string", "bool",
                $"字符串 '{stringValue}' 不是有效的布尔值（只接受 'true' 或 'false'，不区分大小写）");
        }

        // null 为 false，其他对象为 true
        return value != null;
    }
}