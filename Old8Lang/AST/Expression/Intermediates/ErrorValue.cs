using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

public class ErrorValue(Old8Exception value) : ValueType
{
    public override string ToString() => $"{value.Message}";
}