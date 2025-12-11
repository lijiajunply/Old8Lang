using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// bool
/// </summary>
/// <param name="value"></param>
/// <param name="position"></param>
public class BoolLangValue(bool value, SourcePosition position = default) : LangValueType(position)
{
    public readonly bool Value = value;
    
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    public override string ToString() => Value.ToString();
    public override LangValueType Run(VariateManager manager) => this;

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is BoolLangValue b)
            return Value == b.Value;
        return false;
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value) throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => new IntLangValue(Value ? 1 : 0),
            "Bool" or "bool" => this,
            "String" or "string" => new StringLangValue(Value.ToString()),
            "char" or "Char" => new CharLangValue(Value ? '1' : '0'),
            "Double" or "double" => new DoubleLangValue(Value ? 1.0 : 0.0),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }
    
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
    }

    public override Type OutputType(LocalManager local) => Value.GetType();
}