using Old8Lang.AST.Expression;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步 for-in 语句
/// 支持语法：async for item in asyncStream { ... }
/// 类似于 C# 的 await foreach
/// </summary>
public partial class AsyncForInStatement(
    LangId id,
    LangExpression expression,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    public LangId Id => id;
    public LangExpression Expression => expression;
    public OldStatement Body => body;

    // 获取所有标识符，包括主标识符和附加标识符
    internal List<LangId> AllIds { get; } = [id, .. (additionalIds ?? [])];

    /// <summary>
    /// 循环绑定的全部标识符（首个标识符 + 附加标识符）
    /// </summary>
    /// <remarks>
    /// 形如 <c>async for key, value in stream</c> 的循环会绑定两个标识符，
    /// 各执行模式需要据此决定是否对迭代元素做解构（与 <see cref="ForInStatement.Ids"/> 对齐）。
    /// </remarks>
    public IReadOnlyList<LangId> Ids => AllIds;







    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitAsyncForInStatement(this);

    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;

}