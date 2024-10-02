using System.Globalization;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class DoubleValue(double doubleValue) : ValueType
{
    public double Value { get; } = doubleValue;

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
    
    public override ValueType Converse(ValueType otherValueType, ref VariateManager _)
    {
        if (otherValueType is not TypeValue value) throw new Exception("the value is not a type");

        return value.Value switch
        {
            "Int" or "int" => new IntValue((int)Value),
            "Bool" or "bool" => new BoolValue(Value > 0),
            "String" or "string" => new StringValue(Value.ToString(CultureInfo.InvariantCulture)),
            "char" or "Char" => throw new Exception("can not convert double to char"),
            "Double" or "double" => this,
            _ => throw new Exception("not fount the type: " + value.Value)
        };
    }
}