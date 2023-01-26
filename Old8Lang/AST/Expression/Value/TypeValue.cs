using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class TypeValue : ValueType
{
    private OldExpr Expr { get; set; }

    public TypeValue(OldExpr expr) => Expr = expr;

    public override ValueType Run(ref VariateManager Manager)
    {
        var result = Expr.Run(ref Manager);
        return new StringValue(result.TypeToString());
    }
    public override string ToString() => $"typeof({Expr})";
}