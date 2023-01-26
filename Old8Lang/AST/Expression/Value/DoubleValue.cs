namespace Old8Lang.AST.Expression.Value;

public class DoubleValue : ValueType
{
    public new double Value { get; set; }
    public DoubleValue(double doubleValue) => Value = doubleValue;

    public override ValueType Plus(ValueType otherValueType)
    {
        if (otherValueType is IntValue || otherValueType is DoubleValue)
            return new DoubleValue(Value + (double)otherValueType.Value);
        return new ValueType();
    }

    public override ValueType Minus(ValueType otherValueType)
    {
        if (otherValueType is IntValue || otherValueType is DoubleValue)
            return new DoubleValue(Value - (double)otherValueType.Value);
        return new ValueType();
    }

    public override ValueType Times(ValueType otherValueType)
    {
        if (otherValueType is IntValue || otherValueType is DoubleValue)
            return new DoubleValue(Value * (double)otherValueType.Value);
        return new ValueType();
    }

    public override ValueType Divide(ValueType otherValueType)
    {
        if (otherValueType is IntValue || otherValueType is DoubleValue)
            return new DoubleValue(Value / (double)otherValueType.Value);
        return new ValueType();
    }
    

    public override bool Less(ValueType? otherValue) => Value < (double)otherValue.Value;
    public override bool Greater(ValueType? otherValue) => Value > (double)otherValue.Value;
    public override string ToString() => Value.ToString();
    
    public override bool Equal(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue b)
            return Value == b.Value;
        return false;
    }
    public override object GetValue() => Value;
}