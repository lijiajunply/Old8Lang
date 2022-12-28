namespace Old8Lang.CslyMake.OldExpression;

public class OldInt : OldValue
{
    new int Value { get; set; }
    public OldInt(int intValue) => Value = intValue;
}