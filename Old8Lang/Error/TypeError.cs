using Old8Lang.AST;

namespace Old8Lang.Error;

public class TypeError : ErrorException
{
    public TypeError(OldLangTree statement,OldLangTree value) : base(statement,value,"Type is Error") { }
}