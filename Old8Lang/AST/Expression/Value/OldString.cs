using System.Text;

namespace Old8Lang.AST.Expression.Value;

public class OldString : OldValue
{
    new string Value { get; set; }
    public OldString(string context) => Value = context;
    public override string ToString() => Value;
    public override OldValue Plus(OldValue otherValue)
    {
        return new OldString(Value + otherValue.ToString());
    }
    public override bool Equal(OldValue otherValue)
    {
        if (otherValue is OldString b)
            return Value == b.Value;
        return false;
    }
    public override OldValue Times(OldValue otherValue)
    {
        if (otherValue is OldInt)
        {
            OldInt other = otherValue as OldInt;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < (int)other.Value; i++)
            {
                sb.Append(Value);
            }

            return new OldString(sb.ToString());
        }

        return new OldValue();
    }
    public override object GetValue() => Value;
}