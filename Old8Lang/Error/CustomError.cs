using Old8Lang.AST;

namespace Old8Lang.Error;

public class CustomError : Old8Exception
{
    public CustomError(string errorCode, string message, SourcePosition position, IOldLangTree? node = null,
        string? suggestion = null, string[]? sourceContext = null) : base(errorCode, message, position, node,
        suggestion, sourceContext)
    {
    }

    public CustomError(string message, IOldLangTree node, string? suggestion = null,
        string[]? sourceContext = null) : base("RUNTIME_ERROR",message, node, suggestion, sourceContext)
    {
    }
}