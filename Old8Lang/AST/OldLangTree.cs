using sly.lexer;

namespace Old8Lang.AST;

public interface OldLangTree
{
    public LexerPosition Position { get; set; }
}