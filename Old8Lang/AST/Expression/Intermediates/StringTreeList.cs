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
        // 这个方法被调用时，list 中的元素已经是解析后的表达式，
        // 而不是原始的字符串模板。这是因为 ParseStringTree 方法已经将
        // 字符串模板中的占位符分解为了 LangId 对象。
        // 
        // 所以我们只需要直接执行每个表达式，然后将结果拼接起来即可。
        var result = string.Empty;
        
        foreach (var item in list)
        {
            var exprResult = item.Run(manager);
            result += exprResult.ToDisplayString();
        }

        return new StringLangValue(result);
    }
}