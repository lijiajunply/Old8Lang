namespace Old8Lang.CslyMake.OldExpression;

public class OldChar : OldValue
{
    new char Value { get; set; }
    public OldChar(char value) => Value = value;
    public override OldValue PLUS(OldValue otherValue) =>new OldString(Value + (string)otherValue.Value);

    public override OldValue TIMES(OldValue otherValue)
    {
        return new OldString(Value + otherValue.ToString());
    }

    public override string ToString() => Value.ToString();
}