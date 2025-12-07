using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class TypeValue : ValueType
{
    private readonly OldExpr? Expr;
    public string? Value { get; private set; }

    public TypeValue(OldExpr expr) => Expr = expr;
    public TypeValue(string value) => Value = value;

    public override ValueType Run(LangParser.VariateManager manager)
    {
        var result = Expr?.Run(manager);
        if (result == null) throw new InvalidOperationError(this, "类型表达式求值失败");
        Value = result.TypeToString();
        return this;
    }

    public override string ToString() => Value ?? "";
    public override object GetValue() => Value ?? "";
}