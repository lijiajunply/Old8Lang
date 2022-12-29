using sly.lexer;

namespace Old8Lang.CslyMake.OldExpression;

public interface OldLangTree
{
    public LexerPosition Location { get; set; }
}