namespace Old8Lang.AST.Expression.Value;

public class CharValue(char value) : ValueType
{
    private char Value { get; set; } = value;
    public override ValueType Plus(ValueType otherValueType) => new StringValue(Value + (string)otherValueType.GetValue());

    public override ValueType Times(ValueType otherValueType)
    {
        return new StringValue(Value + otherValueType.ToString());
    }

    public override string ToString() => Value.ToString();

    public override bool Equal(ValueType otherValueType)
    {
        if (otherValueType is CharValue b)
            return Value == b.Value;
        return false;
    }

    public override object GetValue() => Value;
}