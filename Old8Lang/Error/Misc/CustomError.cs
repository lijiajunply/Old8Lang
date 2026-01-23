using Old8Lang.AST;

namespace Old8Lang.Error;

public class CustomError(IOldLangTree node, string message) : RuntimeError(node, "RUNTIME_ERROR", message);