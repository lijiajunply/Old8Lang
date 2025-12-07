using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.Intermediates;

public class AsValue(OldExpr id, OldId asId, SourcePosition position = default) : ValueType(position)
{
    public override ValueType Run(LangParser.VariateManager manager)
    {
        var value = id.Run(manager);
        var type = manager.GetAny(asId);

        type ??= new TypeValue(asId.IdName);
        return value.Converse(type, manager);
    }
}