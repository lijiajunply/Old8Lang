using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 字符串插值
/// </summary>
/// <param name="list"></param>
/// <param name="position"></param>
public partial class StringTemplateValue(List<LangExpression> list, SourcePosition position = default) : LangExpression(position)
{
    /// <summary>
    /// 字符串模板中的表达式列表
    /// </summary>
    public List<LangExpression> ExpressionList { get; } = list;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitStringTemplateValue(this);
}