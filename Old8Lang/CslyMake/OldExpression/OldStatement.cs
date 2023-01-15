using System.Text;
using Old8Lang.CslyMake.OldLandParser;
using sly.lexer;

namespace Old8Lang.CslyMake.OldExpression;

public class OldStatement : OldLangTree
{
    public LexerPosition Location { get; set; }

    public virtual void Run(ref VariateManager Manager)
    {
    }
}