using Old8Lang.AST;

namespace Old8Lang.Error;

public class TypeError(IOldLangTree statement, IOldLangTree value) : ErrorException(statement, value, "Type is Error");