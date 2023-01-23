namespace Old8Lang.AST.Expression.Value;

public class OldDouble : OldValue
{
    public new double Value { get; set; }
    public OldDouble(double doubleValue) => Value = doubleValue;

    public override OldValue Plus(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value + (double)otherValue.Value);
        return new OldValue();
    }

    public override OldValue Minus(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value - (double)otherValue.Value);
        return new OldValue();
    }

    public override OldValue Times(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value * (double)otherValue.Value);
        return new OldValue();
    }

    public override OldValue Divide(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value / (double)otherValue.Value);
        return new OldValue();
    }
    

    public override bool Less(OldValue? otherValue) => Value < (double)otherValue.Value;
    public override bool Greater(OldValue? otherValue) => Value > (double)otherValue.Value;
    public override string ToString() => Value.ToString();
    public override bool Equal(OldValue? otherValue) => double.Parse(Value.ToString())  == double.Parse((string)otherValue.ToString()) ;
}