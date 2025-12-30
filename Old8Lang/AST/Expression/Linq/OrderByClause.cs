using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ OrderBy 子句
/// 例如: orderby x ascending, y descending
/// </summary>
public class OrderByClause(List<OrderingItem> orderings, SourcePosition position = default)
    : LinqClause(position)
{
    /// <summary>
    /// 排序项列表
    /// </summary>
    public List<OrderingItem> Orderings { get; set; } = orderings;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 排序项
/// </summary>
public class OrderingItem
{
    /// <summary>
    /// 排序键表达式
    /// </summary>
    public LangExpression KeyExpression { get; set; }

    /// <summary>
    /// 是否升序（true: ascending, false: descending）
    /// </summary>
    public bool IsAscending { get; set; }

    public OrderingItem(LangExpression keyExpression, bool isAscending = true)
    {
        KeyExpression = keyExpression;
        IsAscending = isAscending;
    }
}