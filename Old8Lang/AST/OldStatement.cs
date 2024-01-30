using Old8Lang.CslyParser;
using sly.lexer;

namespace Old8Lang.AST;

public class OldStatement : OldLangTree
{
    public virtual void Run(ref VariateManager Manager){}
    public LexerPosition Position { get; set; } = new();
}