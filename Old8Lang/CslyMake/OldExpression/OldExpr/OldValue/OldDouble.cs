using sly.parser.generator.visitor.dotgraph;

namespace Old8Lang.CslyMake.OldExpression;

public class OldDouble : OldValue
{
    public new double Value { get; set; }
    public OldDouble(double doubleValue) => Value = doubleValue;

    public override OldValue PLUS(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value + (double)otherValue.Value);
        return new OldValue();
    }

    public override OldValue MINUS(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value - (double)otherValue.Value);
        return new OldValue();
    }

    public override OldValue TIMES(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value * (double)otherValue.Value);
        return new OldValue();
    }

    public override OldValue DIVIDE(OldValue otherValue)
    {
        if (otherValue is OldInt || otherValue is OldDouble)
            return new OldDouble(Value / (double)otherValue.Value);
        return new OldValue();
    }
    

    public override bool LESS(OldValue otherValue) => Value < (double)otherValue.Value;
    public override bool GREATER(OldValue otherValue) => Value > (double)otherValue.Value;
    public override string ToString() => Value.ToString();
}