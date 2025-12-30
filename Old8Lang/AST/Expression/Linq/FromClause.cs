using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ From 子句
/// 例如: from x in collection
/// </summary>
public class FromClause(
    string rangeVariable,
    LangExpression dataSource,
    string? typeAnnotation = null,
    SourcePosition position = default)
    : LinqClause(position)
{
    /// <summary>
    /// 范围变量名（例如 x）
    /// </summary>
    public string RangeVariable { get; set; } = rangeVariable;

    /// <summary>
    /// 数据源表达式
    /// </summary>
    public LangExpression DataSource { get; set; } = dataSource;

    /// <summary>
    /// 可选的类型注解
    /// </summary>
    public string? TypeAnnotation { get; set; } = typeAnnotation;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}