using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 字符串插值
/// </summary>
/// <param name="list"></param>
/// <param name="position"></param>
public class StringTreeList(List<OldExpr> list, SourcePosition position = default) : LangValueType(position)
{
    public override LangValueType Run(LangParser.VariateManager manager)
    {
        var result = list.Select(item => item.Run(manager))
            .Aggregate("", (current, value) => current + value.ToDisplayString());

        return new StringLangValue(result);
    }
}