using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class CharValue(char value, SourcePosition position = default) : ValueType(position)
{
    public readonly char Value = value;

    public override ValueType Plus(ValueType otherValueType) =>
        new StringValue(Value + (string)otherValueType.GetValue());

    public override ValueType Times(ValueType otherValueType)
    {
        return new StringValue(Value + otherValueType.ToString());
    }

    public override string ToString() => Value.ToString();

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is CharValue b)
            return Value == b.Value;
        return false;
    }

    public override object GetValue() => Value;

    public override ValueType Converse(ValueType otherValueType, VariateManager manager)
    {
        if (otherValueType is not TypeValue value) throw new TypeError(this, "TypeValue", otherValueType.GetType().Name);

        return value.Value switch
        {
            "Int" or "int" => new IntValue(Convert.ToInt32(Value)),
            "Bool" or "bool" => throw new TypeError(this, "bool", "无法将字符转换为布尔值"),
            "String" or "string" => new StringValue(Value.ToString()),
            "char" or "Char" => this,
            "Double" or "double" => new DoubleValue(Convert.ToDouble(Value)),
            _ => throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}")
        };
    }
    
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, Convert.ToInt32(Value));
    }

    public override Type OutputType(LocalManager local) => Value.GetType();
}