namespace Old8Lang.CslyMake.OldExpression;

public class OldDouble : OldValue
{
    public new double Value { get; set; }
    public OldDouble(double doubleValue) => Value = doubleValue;
}