using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

public class TypeValue : ValueType
{
    private readonly OldExpr? Expr;
    public string? Value { get; private set; }

    public TypeValue(OldExpr expr) => Expr = expr;
    public TypeValue(string value) => Value = value;

    public override ValueType Run(ref VariateManager Manager)
    {
        var result = Expr?.Run(ref Manager);
        if (result == null) return new VoidValue();
        Value = result.TypeToString();
        return this;
    }

    public override string ToString() => Value ?? "";
    public override object GetValue() => Value ?? "";
}