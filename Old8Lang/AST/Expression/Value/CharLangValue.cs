using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class CharLangValue(char value, SourcePosition position = default) : LangValueType(position)
{
    public readonly char Value = value;

    public override LangValueType Plus(LangValueType otherLangValueType) =>
        new StringLangValue(Value + (string)otherLangValueType.GetValue());

    public override LangValueType Times(LangValueType otherLangValueType)
    {
        return new StringLangValue(Value + otherLangValueType.ToString());
    }

    public override string ToString() => Value.ToString();

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is CharLangValue b)
            return Value == b.Value;
        return false;
    }

    public override object GetValue() => Value;

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value) throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => new IntLangValue(Convert.ToInt32(Value)),
            "Bool" or "bool" => throw new TypeError(this, "bool", "无法将字符转换为布尔值"),
            "String" or "string" => new StringLangValue(Value.ToString()),
            "char" or "Char" => this,
            "Double" or "double" => new DoubleLangValue(Convert.ToDouble(Value)),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }
    
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, Convert.ToInt32(Value));
    }

    public override Type OutputType(LocalManager local) => Value.GetType();
}