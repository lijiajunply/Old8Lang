using Old8Lang.OldLandParser;
using sly.lexer;

namespace Old8Lang.AST;

public class OldStatement : OldLangTree
{
    public LexerPosition Location { get; set; }

    public virtual void Run(ref VariateManager Manager)
    {
    }
}