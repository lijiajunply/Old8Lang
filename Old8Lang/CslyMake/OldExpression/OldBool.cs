namespace Old8Lang.CslyMake.OldExpression;

public class OldBool : OldValue
{
    new bool Value { get; set; }
    public OldBool(bool boolValue) => Value = boolValue;
}