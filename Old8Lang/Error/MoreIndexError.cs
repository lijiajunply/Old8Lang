using Old8Lang.AST;

namespace Old8Lang.Error;

public class MoreIndexError(OldLangTree statement, OldLangTree value)
    : ErrorException(statement, value, "index is more that the max!");