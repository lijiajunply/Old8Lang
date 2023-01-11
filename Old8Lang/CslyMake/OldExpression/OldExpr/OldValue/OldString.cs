using System.Text;

namespace Old8Lang.CslyMake.OldExpression;

public class OldString : OldValue
{
    new string Value { get; set; }
    public OldString(string stringvalue) => Value = stringvalue;
    public override string ToString() => Value;
    public override OldValue PLUS(OldValue otherValue)
    {
        return new OldString(Value + otherValue.ToString());
    }
    public override bool EQUAL(OldValue otherValue) => Value == otherValue.ToString();
    public override OldValue TIMES(OldValue otherValue)
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
}