using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldBool : OldValue
{
    public new bool Value { get; set; }
    public OldBool(bool value) => Value = value;
    public override string ToString() => Value.ToString();
    public override OldValue Run(ref VariateManager Manager) => this;
    public override bool Equal(OldValue? otherValue) => bool.Parse(Value.ToString()) == bool.Parse((string)otherValue.ToString());
}