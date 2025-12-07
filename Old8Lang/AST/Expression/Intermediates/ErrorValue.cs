namespace Old8Lang.AST.Expression.Intermediates;

public class ErrorValue(IOldLangTree statement, IOldLangTree value) : ValueType
{
    public override string ToString() => $"Error at {statement.Position}: Invalid expression";
}