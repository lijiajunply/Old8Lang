namespace Old8Lang.CslyMake.OldExpression;

public class OldBool : OldValue
{
    public bool BoolValue { get; set; }
    public OldBool(bool boolValue) => BoolValue = boolValue;
}