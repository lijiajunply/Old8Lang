using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.Intermediates;

public class AsLangValue(OldExpr id, LangId asId, SourcePosition position = default) : LangValueType(position)
{
    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var value = id.Run(manager);
        var type = manager.GetAny(asId);

        type ??= new TypeLangValue(asId.IdName);
        return value.Converse(type, manager);
    }
}