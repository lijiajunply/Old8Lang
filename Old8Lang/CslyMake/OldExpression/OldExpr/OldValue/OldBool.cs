using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldBool : OldValue
{
    public new bool Value { get; set; }
    public OldBool(bool value) => Value = value;
    public override string ToString() => Value.ToString();
    public override OldValue Run(ref VariateManager Manager) => this;
    public override bool EQUAL(OldValue otherValue) => bool.Parse(Value.ToString()) == bool.Parse(otherValue.ToString());
}