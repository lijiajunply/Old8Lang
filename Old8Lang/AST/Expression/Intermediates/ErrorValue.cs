using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

public class ErrorValue(IOldLangTree statement, IOldLangTree value) : ValueType
{
    private readonly ErrorException ErrorException = new(statement, value);

    public override string ToString() => ErrorException.Message;
}