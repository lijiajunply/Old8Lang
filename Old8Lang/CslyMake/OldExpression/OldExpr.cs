using Old8Lang.CslyMake.OldLandParser;
using sly.lexer;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// expr ::= id compare value 
/// </summary>
public class OldExpr : OldLangTree
{
    public LexerPosition Location { get; set; }
    public OldTokenGeneric Compare { get; set; }

    public virtual OldExpr Run(ref VariateManager Manager)
    {
        return null;
    }
}