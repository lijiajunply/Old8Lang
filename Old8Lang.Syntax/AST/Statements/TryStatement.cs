using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// try 语句，用于异常处理。
/// </summary>
/// <param name="tryBlock">try块中的语句</param>
/// <param name="catchBlocks">catch块列表，每个catch块包含异常类型、变量名、过滤器和处理语句</param>
/// <param name="finallyBlock">finally块中的语句</param>
/// <param name="position">位置信息</param>
public partial class TryStatement(
    BlockStatement tryBlock,
    List<(string? exceptionType, LangId? exceptionVar, LangExpression? filter, BlockStatement catchBlock)> catchBlocks,
    BlockStatement? finallyBlock = null,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 获取 try 块
    /// </summary>
    public BlockStatement TryBlock => tryBlock;

    /// <summary>
    /// 获取 catch 块列表
    /// </summary>
    public List<(string? exceptionType, LangId? exceptionVar, LangExpression? filter, BlockStatement catchBlock)>
        CatchBlocks => catchBlocks;

    /// <summary>
    /// 获取 finally 块
    /// </summary>
    public BlockStatement? FinallyBlock => finallyBlock;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitTryStatement(this);

    public override OldStatement this[int index]
    {
        get
        {
            // 返回 try 块、catch 块、finally 块
            // 顺序：try块(0), catch块(1...N), finally块(最后)
            if (index == 0)
            {
                return tryBlock;
            }

            // catch 块
            int catchCount = catchBlocks.Count;
            if (index <= catchCount)
            {
                return catchBlocks[index - 1].catchBlock;
            }

            // finally 块
            if (index == catchCount + 1 && finallyBlock is not null)
            {
                return finallyBlock;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<OldStatement>());
        }
    }

    public override int Count
    {
        get
        {
            int count = 1; // try 块
            count += catchBlocks.Count; // catch 块
            if (finallyBlock is not null)
            {
                count++; // finally 块
            }

            return count;
        }
    }
}
