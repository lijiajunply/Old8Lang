using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class TupleValue(OldExpr v1, OldExpr v2) : ValueType
{
    public ValueTuple<ValueType, ValueType> Value { get; private set; }

    public override ValueType Run(VariateManager Manager)
    {
        Value = (v1.Run(Manager), v2.Run(Manager));
        return this;
    }

    public override string ToString() => Value is (null, null) ? $"({v1},{v2})" : $"({Value.Item1},{Value.Item2})";
    public override object GetValue() => (Value.Item1.GetValue(), Value.Item2.GetValue());
}