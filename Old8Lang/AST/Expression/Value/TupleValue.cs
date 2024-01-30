using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class TupleValue(OldExpr v1, OldExpr v2) : ValueType
{
    public ValueTuple<ValueType, ValueType> Value { get; private set; }

    private OldExpr V1 { get; set; } = v1;

    private OldExpr V2 { get; set; } = v2;

    public override ValueType Run(ref VariateManager Manager)
    {
        Value = (V1.Run(ref Manager), V2.Run(ref Manager));
        return this;
    }

    public override string ToString() => Value is (null, null) ? $"({V1},{V2})" : $"({Value.Item1},{Value.Item2})";
    public override object GetValue() => (Value.Item1.GetValue(), Value.Item2.GetValue());
}