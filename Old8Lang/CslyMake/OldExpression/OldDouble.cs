namespace Old8Lang.CslyMake.OldExpression;

public class OldDouble : OldValue
{
    public double DoubleValue { get; set; }
    public OldDouble(double doubleValue) => DoubleValue = doubleValue;
}