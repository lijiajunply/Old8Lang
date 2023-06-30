using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class ErrorValue : ValueType
{
    private ErrorException ErrorException { get; set; }

    public ErrorValue(OldLangTree statement, OldLangTree value) =>
        ErrorException = new ErrorException(statement, value);

    public override string ToString() => ErrorException.Message;
}