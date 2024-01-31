using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class BoolValue(bool value) : ValueType
{
    public bool Value { get; set; } = value;
    public override string ToString() => Value.ToString();
    public override ValueType Run(ref VariateManager Manager) => this;

    public override bool Equal(ValueType? otherValueType)
    {
        if (otherValueType is BoolValue b)
            return Value == b.Value;
        return false;
    }
}