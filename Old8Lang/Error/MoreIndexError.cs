using Old8Lang.AST;

namespace Old8Lang.Error;

public class MoreIndexError(IOldLangTree statement, IOldLangTree value)
    : ErrorException(statement, value, "index is more that the max!");