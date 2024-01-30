using Old8Lang.AST;

namespace Old8Lang.Error;

public class TypeError(OldLangTree statement, OldLangTree value) : ErrorException(statement, value, "Type is Error");