using Old8Lang.AST;

namespace Old8Lang.Error;

public class ErrorException : Exception
{
    public ErrorException(IOldLangTree statement,IOldLangTree value) :
        base($"{statement} is error message is{value}"){}
    protected ErrorException(IOldLangTree statement,IOldLangTree value,string errorMessage) :
        base($"{statement} is error at {value} \n {errorMessage}") {}
}