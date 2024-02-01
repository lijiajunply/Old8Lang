using Old8Lang.AST;

namespace Old8Lang.Error;

public class ErrorException : Exception
{
    public ErrorException(OldLangTree statement,OldLangTree value) :
        base($"{statement} is error message is{value}"){}
    protected ErrorException(OldLangTree statement,OldLangTree value,string errorMessage) :
        base($"{statement} is error at {value} \n {errorMessage}") {}
}