using Old8Lang.AST;

namespace Old8Lang.Error;

public class MoreIndexError : OldError
{
    public MoreIndexError(OldLangTree statement,OldLangTree value) : base(statement,value,"index is more that the max!") { }
}