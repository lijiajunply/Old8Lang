using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class ErrorValue : ValueType
{
    private OldError Error { get; set; }

    public ErrorValue(OldLangTree statement,OldLangTree value) => Error = new OldError(statement,value);
    public override string ToString() => Error.Message;
}