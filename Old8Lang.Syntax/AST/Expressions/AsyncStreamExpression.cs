using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 异步流表达式：<c>async { block }</c>，创建一个异步流（异步生成器）。
/// </summary>
public partial class AsyncStreamExpression(BlockStatement block, SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>异步流体。</summary>
    public BlockStatement Block { get; } = block;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitAsyncStreamExpression(this);

    public override string ToString() => $"async {{ {Block} }}";
}
