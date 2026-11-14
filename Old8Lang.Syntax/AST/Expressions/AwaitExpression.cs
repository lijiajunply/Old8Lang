using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// await 表达式
/// 用于等待异步操作完成并获取结果
/// </summary>
public partial class AwaitExpression : LangExpression
{
    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitAwaitExpression(this);

    public readonly LangExpression Expression;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AwaitExpression(LangExpression expression, SourcePosition position = default)
        : base(position)
    {
        Expression = expression;
    }


}