using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class IntValue(int intValue) : ValueType
{
    public int Value { get; set; } = intValue;
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
            return new IntValue(Value + int.Parse(otherValueType.ToString()));

        return new VoidValue();
    }

    public override ValueType Minus(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue)
            return otherValueType.Minus(this);
        if (otherValueType is IntValue)
            return new IntValue(Value - int.Parse(otherValueType.ToString()));
        return new VoidValue();
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
            return new IntValue(Value * int.Parse(otherValueType.ToString()));
        return new VoidValue();
    }

    public override ValueType Divide(ValueType otherValueType)
    {
        if (otherValueType is DoubleValue)
            return otherValueType.Divide(this);
        if (otherValueType is IntValue)
            return new IntValue(Value / int.Parse(otherValueType.ToString()));
        return new VoidValue();
    }

    public override bool Less(ValueType? otherValue) => Value < int.Parse(otherValue!.ToString());
    public override bool Greater(ValueType? otherValue) => Value > int.Parse(otherValue!.ToString());

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is IntValue b)
            return Value == b.Value;
        return false;
    }

    public override ValueType Converse(ValueType otherValueType, ref VariateManager _)
    {
        if (otherValueType is not TypeValue value) throw new Exception("the value is not a type");

        return value.Value switch
        {
            "Int" or "int" => this,
            "Bool" or "bool" => new BoolValue(Value > 0),
            "String" or "string" => new StringValue(Value.ToString()),
            "char" or "Char" => new CharValue(Convert.ToChar(Value)),
            "Double" or "double" => new DoubleValue(Value),
            _ => throw new Exception("not fount the type: " + value.Value)
        };
    }
    
    public override object GetValue() => Value;
}