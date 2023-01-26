using System.ComponentModel.DataAnnotations;
using Old8Lang.AST;
using Old8Lang.AST.Expression;

namespace Old8Lang.Error;

public class ErrorException : Exception
{
    public ErrorException(OldLangTree statement,OldLangTree value) :
        base($"{statement} is error message is{value} at {statement.Position}:{value.Position}"){}
    protected ErrorException(OldLangTree statement,OldLangTree value,string errorMessage) :
        base($"{statement} is error at {value} \nat {statement.Position}:{value.Position} {errorMessage}") {}
}