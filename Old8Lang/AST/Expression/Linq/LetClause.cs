using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ Let 子句
/// 例如: let squared <- x * x
/// </summary>
public class LetClause : LinqClause
{
    /// <summary>
    /// 变量名
    /// </summary>
    public string Variable { get; set; }

    /// <summary>
    /// 赋值表达式
    /// </summary>
    public LangExpression Expression { get; set; }

    public LetClause(string variable, LangExpression expression, SourcePosition position = default) : base(position)
    {
        Variable = variable;
        Expression = expression;
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}
