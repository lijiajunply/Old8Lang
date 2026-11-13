using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ Let 子句
/// 例如: let squared &lt;- x * x
/// </summary>
public class LetClause(string variable, LangExpression expression, SourcePosition position = default)
    : LinqClause(position)
{
    /// <summary>
    /// 变量名
    /// </summary>
    public string Variable { get; set; } = variable;

    /// <summary>
    /// 赋值表达式
    /// </summary>
    public LangExpression Expression { get; set; } = expression;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // LetClause 是 LINQ 查询的内部组成部分，不应该被独立访问
        throw new InvalidOperationException("LetClause 不应该被独立访问，应该通过 LinqExpression 处理");
    }
}
