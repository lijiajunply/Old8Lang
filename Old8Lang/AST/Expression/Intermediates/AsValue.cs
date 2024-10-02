using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST.Statement;

public class AsValue(OldExpr id, OldID asId) : ValueType
{
    private OldExpr Id { get; } = id;
    private OldID AsId { get; } = asId;

    public override ValueType Run(ref VariateManager Manager)
    {
        var value = Id.Run(ref Manager);
        var type = Manager.GetAny(AsId);
        
        type ??= new TypeValue(AsId.IdName);
        return value.Converse(type, ref Manager);
    }
}