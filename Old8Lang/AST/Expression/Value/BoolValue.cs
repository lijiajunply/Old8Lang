using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class BoolValue : ValueType
{
    public new bool Value { get; set; }
    public BoolValue(bool value) => Value = value;
    public override string ToString() => Value.ToString();
    public override ValueType Run(ref VariateManager Manager) => this;

    public override bool Equal(ValueType otherValueType)
    {
        if (otherValueType is BoolValue b)
            return Value == b.Value;
        return false;
    }
}