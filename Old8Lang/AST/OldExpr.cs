using Old8Lang.AST.Expression;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST;
/// <summary>
/// expr ::= id compare value 
/// </summary>
public class OldExpr : OldLangTree
{
    public OldTokenGeneric Compare { get; set; }

    public virtual OldValue Run(ref VariateManager Manager) => null;

}