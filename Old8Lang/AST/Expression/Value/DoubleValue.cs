using System.Globalization;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class DoubleValue(double doubleValue) : ValueType
{
    public readonly double Value = doubleValue;

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
        if (otherValue is DoubleValue d)
            return Value < d.Value;
        if (otherValue is IntValue i)
            return Value < i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
    }

    public override bool Greater(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value > d.Value;
        if (otherValue is IntValue i)
            return Value > i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
    }

    public override bool LessEqual(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value <= d.Value;
        if (otherValue is IntValue i)
            return Value <= i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
    }

    public override bool GreaterEqual(ValueType? otherValue)
    {
        if (otherValue is DoubleValue d)
            return Value >= d.Value;
        if (otherValue is IntValue i)
            return Value >= i.Value;
        throw new Exception("not fount the type: " + otherValue!.TypeToString());
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