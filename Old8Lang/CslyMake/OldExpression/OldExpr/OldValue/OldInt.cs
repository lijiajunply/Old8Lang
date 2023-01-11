namespace Old8Lang.CslyMake.OldExpression;

public class OldInt : OldValue
{
    public new int Value { get; set; }
    public OldInt(int intValue) => Value = intValue;
    public override string ToString() => Value.ToString();
    public override OldValue PLUS(OldValue otherValue)
    {
        if (otherValue is OldString)
            return otherValue.PLUS(this);
        if (otherValue is OldChar)
            return otherValue.PLUS(this);
        if (otherValue is OldDouble)
            return otherValue.PLUS(this);
        if (otherValue is OldInt)
        {
            var other = otherValue as OldInt;
            return new OldInt((Value + other.Value));
        }
            
        return new OldValue();
    }

    public override OldValue MINUS(OldValue otherValue)
    {
        if (otherValue is OldDouble)
            return otherValue.MINUS(this);
        if (otherValue is OldInt)
            return new OldInt(Value - Int32.Parse(otherValue.ToString()));
        return new OldValue();
    }

    public override OldValue TIMES(OldValue otherValue)
    {
        if (otherValue is OldString)
            return otherValue.TIMES(this);
        if (otherValue is OldChar)
            return otherValue.TIMES(this);
        if (otherValue is OldDouble)
            return otherValue.TIMES(this);
        if (otherValue is OldInt)
            return new OldInt(Value * (int)otherValue.Value);
        return new OldValue();
    }

    public override OldValue DIVIDE(OldValue otherValue)
    {
        if (otherValue is OldDouble)
            return otherValue.DIVIDE(this);
        if (otherValue is OldInt)
            return new OldInt(Value / (int)otherValue.Value);
        return new OldValue();
    }

    public override bool LESS(OldValue otherValue) => Value < Int32.Parse(otherValue.ToString());
    public override bool GREATER(OldValue otherValue) => Value > Int32.Parse(otherValue.ToString());
    public override bool EQUAL(OldValue otherValue) => Value == Int32.Parse(otherValue.ToString());
}