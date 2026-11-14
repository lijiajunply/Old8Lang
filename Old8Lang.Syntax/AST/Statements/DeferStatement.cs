using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// defer 语句，用于延迟执行（类似于 Go 的 defer 语句）。
/// 在函数返回前执行，多个 defer 按后进先出（LIFO）顺序执行。
/// </summary>
/// <param name="statement">要延迟执行的语句</param>
/// <param name="position">位置信息</param>
public partial class DeferStatement(
    OldStatement statement,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 获取要延迟执行的语句
    /// </summary>
    public OldStatement Statement => statement;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitDeferStatement(this);

    public override OldStatement this[int index]
    {
        get
        {
            // Defer 语句只包含一个子语句
            if (index == 0)
            {
                return statement;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<OldStatement>());
        }
    }

    public override int Count => 1; // 只有一个子语句
}
