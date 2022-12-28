namespace Old8Lang.CslyMake.OldExpression;

public class OldString : OldValue
{
    public string Value { get; set; }
    public OldString(string stringvalue) => Value = stringvalue;
}