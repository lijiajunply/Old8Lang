using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class IntValue : ValueType
{
    public new int Value { get; set; }
    public IntValue(int intValue) => Value = intValue;
    public override string ToString() => Value.ToString();
    public override ValueType Plus(ValueType otherValueType)
    {
        if (otherValueType is StringValue)
            return otherValueType.Plus(this);
        if (otherValueType is CharValue)
            return otherValueType.Plus(this);
        if (otherValueType is DoubleValue)
            return otherValueType.Plus(this);
        if (otherValueType is IntValue)
            return new IntValue(Value + Int32.Parse((string)otherValueType.ToString()));

        return new ValueType();
    }

    public override ValueType Minus(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue)
            return otherValueType.Minus(this);
        if (otherValueType is IntValue)
            return new IntValue(Value - Int32.Parse((string)otherValueType.ToString()));
        return new ValueType();
    }

    public override ValueType Times(ValueType otherValueType)
    {
        if (otherValueType is StringValue)
            return otherValueType.Times(this);
        if (otherValueType is CharValue)
            return otherValueType.Times(this);
        if (otherValueType is DoubleValue)
            return otherValueType.Times(this);
        if (otherValueType is IntValue)
            return new IntValue(Value * Int32.Parse((string)otherValueType.ToString()));
        return new ValueType();
    }

    public override ValueType Divide(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue)
            return otherValueType.Divide(this);
        if (otherValueType is IntValue)
            return new IntValue(Value / Int32.Parse((string)otherValueType.ToString()));
        return new ValueType();
    }

    public override bool Less(ValueType?    otherValue) => Value < Int32.Parse((string)otherValue.ToString());
    public override bool Greater(ValueType? otherValue) => Value > Int32.Parse((string)otherValue.ToString());
    public override bool Equal(ValueType otherValueType)
    {
        if (otherValueType is IntValue b)
            return Value == b.Value;
        return false;
    }
    public override object GetValue() => Value;
}