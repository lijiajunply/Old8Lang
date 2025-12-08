using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 转换表达式
/// </summary>
/// <param name="id"></param>
/// <param name="asId"></param>
/// <param name="position"></param>
public class AsLangValue(OldExpr id, LangId asId, SourcePosition position = default) : LangValueType(position)
{
    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var value = id.Run(manager);
        LangValueType? type = manager.GetAny(asId);

        type ??= new TypeLangValue(asId.IdName);
        return value.Converse(type, manager);
    }
}