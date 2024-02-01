using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST;

public class OldExpr : OldLangTree
{
    public virtual ValueType Run(ref VariateManager Manager) => new VoidValue();
}