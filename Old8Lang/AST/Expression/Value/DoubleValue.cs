using System.Globalization;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class DoubleValue(double doubleValue) : ValueType
{
    private double Value { get; set; } = doubleValue;

    public override ValueType Plus(ValueType otherValueType) =>
        otherValueType is IntValue or DoubleValue
            ? new DoubleValue(Value + double.Parse(otherValueType.ToString()))
            : new VoidValue();

    public override ValueType Minus(ValueType otherValueType) =>
        otherValueType is IntValue or DoubleValue
            ? new DoubleValue(Value - double.Parse(otherValueType.ToString()))
            : new VoidValue();

    public override ValueType Times(ValueType otherValueType) =>
        otherValueType is IntValue or DoubleValue
            ? new DoubleValue(Value * double.Parse(otherValueType.ToString()))
            : new VoidValue();

    public override ValueType Divide(ValueType otherValueType) =>
        otherValueType is IntValue or DoubleValue
            ? new DoubleValue(Value / double.Parse(otherValueType.ToString()))
            : new VoidValue();


    public override bool Less(ValueType? otherValue)
    {
        if(double.TryParse(otherValue?.ToString(),out var d))
            return Value < d;
        throw new TypeError(this, this);
    }

    public override bool Greater(ValueType? otherValue) {
        if(double.TryParse(otherValue?.ToString(),out var d))
            return Value > d;
        throw new TypeError(this, this);
    }
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is DoubleValue b)
            return Math.Abs(Value - b.Value) < 0.03;
        return false;
    }

    public override object GetValue() => Value;
}