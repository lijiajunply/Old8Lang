using Old8Lang.CslyParser;

namespace Old8Lang.AST;

public abstract class OldStatement : OldLangTree
{
    public virtual void Run(ref VariateManager Manager){}
}