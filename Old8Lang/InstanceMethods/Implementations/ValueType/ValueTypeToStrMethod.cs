using System.Globalization;
using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType.ToStr() - 转换为字符串表示
/// </summary>
public class ValueTypeToStrMethod : BaseValueTypeMethod
{
    public override string[] Names => ["ToStr", "toStr", "ToString", "toString"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;
    public override Type? DeclaredReturnType => typeof(StringLangValue);

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        return new StringLangValue(instance.ToDisplayString());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ValueTypeToStrMethod).GetMethod(nameof(ConvertToStrHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static string ConvertToStrHelper(LangValueType type)
    {
        return type.ToDisplayString();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 对于 double，如果是整数值，使用固定格式（不使用科学计数法）
        if (instance is double d)
        {
            if (Math.Abs(d - Math.Round(d)) < 0.0000001)
            {
                return d.ToString("F0");
            }
            return d.ToString(CultureInfo.InvariantCulture);
        }

        // 对于 long，直接转换为字符串
        if (instance is long l)
        {
            return l.ToString();
        }

        // 对于 bool，返回小写字符串
        if (instance is bool b)
        {
            return b ? "true" : "false";
        }

        // 对于 LangValueType，使用 ToDisplayString
        if (instance is LangValueType langValue)
        {
            return langValue.ToDisplayString();
        }

        return instance?.ToString() ?? "null";
    }
}
