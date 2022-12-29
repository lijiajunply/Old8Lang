using Old8Lang.CslyMake.OldLandParser;
using sly.lexer;

namespace Old8Lang.CslyMake.OldExpression;

public class OldStatement : OldLangTree
{
    public List<OldID> IDs = new List<OldID>();
    public Dictionary<int, OldValue> Id_Value = new Dictionary<int, OldValue>();
    public LexerPosition Location { get; set; }

    public virtual void Run(ref VariateManager Manager)
    {
        
    }
}