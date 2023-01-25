using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldInt : OldValue
{
    public new int Value { get; set; }
    public OldInt(int intValue) => Value = intValue;
    public override string ToString() => Value.ToString();
    public override OldValue Plus(OldValue otherValue)
    {
        if (otherValue is OldString)
            return otherValue.Plus(this);
        if (otherValue is OldChar)
            return otherValue.Plus(this);
        if (otherValue is OldDouble)
            return otherValue.Plus(this);
        if (otherValue is OldInt)
            return new OldInt(Value + Int32.Parse((string)otherValue.ToString()));

        return new OldValue();
    }

    public override OldValue Minus(OldValue otherValue)
    {
        if (otherValue is OldDouble)
            return otherValue.Minus(this);
        if (otherValue is OldInt)
            return new OldInt(Value - Int32.Parse((string)otherValue.ToString()));
        return new OldValue();
    }

    public override OldValue Times(OldValue otherValue)
    {
        if (otherValue is OldString)
            return otherValue.Times(this);
        if (otherValue is OldChar)
            return otherValue.Times(this);
        if (otherValue is OldDouble)
            return otherValue.Times(this);
        if (otherValue is OldInt)
            return new OldInt(Value * Int32.Parse((string)otherValue.ToString()));
        return new OldValue();
    }

    public override OldValue Divide(OldValue otherValue)
    {
        if (otherValue is OldDouble)
            return otherValue.Divide(this);
        if (otherValue is OldInt)
            return new OldInt(Value / Int32.Parse((string)otherValue.ToString()));
        return new OldValue();
    }

    public override bool Less(OldValue?    otherValue) => Value < Int32.Parse((string)otherValue.ToString());
    public override bool Greater(OldValue? otherValue) => Value > Int32.Parse((string)otherValue.ToString());
    public override bool Equal(OldValue otherValue)
    {
        if (otherValue is OldInt b)
            return Value == b.Value;
        return false;
    }
    public override object GetValue() => Value;
}