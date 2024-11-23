using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST.Statement;

public class AsValue(OldExpr id, OldID asId) : ValueType
{
    public override ValueType Run(ref VariateManager Manager)
    {
        var value = id.Run(ref Manager);
        var type = Manager.GetAny(asId);
        
        type ??= new TypeValue(asId.IdName);
        return value.Converse(type, ref Manager);
    }
}