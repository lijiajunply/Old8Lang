namespace Old8Lang.CslyMake.OldExpression;

public class OldChar : OldValue
{
    new char Value { get; set; }
    public OldChar(char value) => Value = value;
    public override OldValue PLUS(OldValue otherValue) =>new OldString(Value + (string)otherValue.Value);

    public override OldValue TIMES(OldValue otherValue)
    {
        if (otherValue is OldInt)
        {
            int a = (int)otherValue.Value;
            string b = "";
            for (int i = 0; i < a; i++)
            {
                b += Value;
            }
            return new OldString(b);
        }
        return new OldValue();
    }

    public override string ToString() => Value.ToString();
}