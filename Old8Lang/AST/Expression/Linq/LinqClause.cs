using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ 子句基类
/// </summary>
public abstract class LinqClause(SourcePosition position = default) : IOldLangTree
{
    public SourcePosition Position { get; set; } = position;

    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
}