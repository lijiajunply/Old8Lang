using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Null值
/// </summary>
/// <param name="position">位置</param>
public partial class NullLangValue(SourcePosition position = default) : LangValueType(position), ILiteralSyntax
{

    /// <inheritdoc />
    public object? LiteralValue => null;

    /// <summary>
    /// 单例实例，用于减少对象创建
    /// </summary>
    public static readonly NullLangValue Instance = new();

    public override string ToString() => "Null";

    public override bool Equal(LangValueType? otherValueType)
    {
        return otherValueType is NullLangValue;
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Bool" or "bool" => BoolLangValue.Create(false, Position),
            "String" or "string" => StringLangValue.Create("null", Position),
            "Int" or "int" => IntLangValue.Create(0, Position),
            "Double" or "double" => DoubleLangValue.Create(0.0, Position),
            "Char" or "char" => CharLangValue.Create('\0', Position),
            _ => throw new TypeError(this, "不支持的类型转换")
        };
    }

    public override object? GetValue() => null;

}