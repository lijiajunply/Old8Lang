using System.Text;

namespace Old8Lang.AST.Expression.Value;

public class StringValue : ValueType
{
    public string Value { get; }

    public StringValue(string context)
    {
        Value = "";
        var a = context.Split("\\n");
        for (var i = 0; i < a.Length; i++)
            Value += a[i] + (i == a.Length - 1 ? "" : "\n");
    }

    public override string ToString() => Value;
    public override ValueType Plus(ValueType otherValueType) => new StringValue(Value + otherValueType);

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is StringValue b)
            return Value == b.Value;
        return false;
    }

    public override ValueType Times(ValueType otherValueType)
    {
        if (otherValueType is IntValue value)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < value.Value; i++)
                sb.Append(Value);
            return new StringValue(sb.ToString());
        }

        return new VoidValue();
    }

    public override object GetValue() => Value;
}