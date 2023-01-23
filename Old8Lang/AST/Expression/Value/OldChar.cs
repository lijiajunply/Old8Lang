namespace Old8Lang.AST.Expression.Value;

public class OldChar : OldValue
{
    new char Value { get; set; }
    public OldChar(char value) => Value = value;
    public override OldValue Plus(OldValue otherValue) =>new OldString(Value + (string)otherValue.Value);

    public override OldValue Times(OldValue otherValue)
    {
        return new OldString(Value + otherValue.ToString());
    }

    public override string ToString() => Value.ToString();
    public override bool   Equal(OldValue? otherValue) => Value.ToString() == otherValue.ToString();
}