using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class ErrorValue(OldLangTree statement, OldLangTree value) : ValueType
{
    private ErrorException ErrorException => new(statement, value);

    public override string ToString() => ErrorException.Message;
}