using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class CharValue(char value) : ValueType
{
    public char Value { get; } = value;

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

    public override ValueType Converse(ValueType otherValueType, ref VariateManager _)
    {
        if (otherValueType is not TypeValue value) throw new Exception("the value is not a type");

        return value.Value switch
        {
            "Int" or "int" => new IntValue(Convert.ToInt32(Value)),
            "Bool" or "bool" => throw new Exception("can not convert char to bool"),
            "String" or "string" => new StringValue(Value.ToString()),
            "char" or "Char" => this,
            "Double" or "double" => new DoubleValue(Convert.ToDouble(Value)),
            _ => throw new Exception("not fount the type: " + value.Value)
        };
    }
}