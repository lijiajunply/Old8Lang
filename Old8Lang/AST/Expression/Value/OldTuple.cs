using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldTuple : OldValue
{
    public new ValueTuple<OldValue,OldValue> Value { get; set; }

    private OldExpr V1 { get; set; }
    
    private OldExpr V2 { get; set; }

    public OldTuple(OldExpr v1,OldExpr v2)
    {
        V1 = v1;
        V2 = v2;
    }
    public override OldValue Run(ref VariateManager Manager)
    {
        Value = (V1.Run(ref Manager),V2.Run(ref Manager));
        return this;
    }
    public override string ToString() => Value is (null,null)?$"({V1},{V2})":$"({Value.Item1},{Value.Item2})";
    public override object GetValue() => (Value.Item1.GetValue(),Value.Item2.GetValue());
}