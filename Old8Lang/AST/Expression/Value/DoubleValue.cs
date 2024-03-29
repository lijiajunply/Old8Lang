namespace Old8Lang.AST.Expression.Value;

public class DoubleValue : ValueType
{
    public new double Value { get; set; }
    public DoubleValue(double doubleValue) => Value = doubleValue;

    public override ValueType Plus(ValueType otherValueType) => 
        otherValueType is IntValue or DoubleValue ? new DoubleValue(Value+ Double.Parse(otherValueType.ToString())) : new ValueType();

    public override ValueType Minus(ValueType otherValueType) =>
        otherValueType is IntValue or DoubleValue ? new DoubleValue(Value- Double.Parse(otherValueType.ToString())) : new ValueType();

    public override ValueType Times(ValueType otherValueType) => 
        otherValueType is IntValue or DoubleValue ? new DoubleValue(Value * Double.Parse(otherValueType.ToString())) : new ValueType();

    public override ValueType Divide(ValueType otherValueType) =>
        otherValueType is IntValue or DoubleValue ? new DoubleValue(Value / Double.Parse(otherValueType.ToString())) : new ValueType();


    public override bool   Less(ValueType?    otherValue) => Value < Double.Parse(otherValue.ToString());
    public override bool   Greater(ValueType? otherValue) => Value > Double.Parse(otherValue.ToString());
    public override string ToString()                     => Value.ToString();
    
    public override bool Equal(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue b)
            return Value == b.Value;
        return false;
    }
    public override object GetValue() => Value;
}