using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Try语句，用于异常处理
/// </summary>
/// <param name="tryBlock">try块中的语句</param>
/// <param name="catchBlocks">catch块列表，每个catch块包含异常类型和处理语句</param>
/// <param name="finallyBlock">finally块中的语句</param>
/// <param name="position">位置信息</param>
public class TryStatement(
    BlockStatement tryBlock,
    List<(string? exceptionType, OldId? exceptionVar, BlockStatement catchBlock)> catchBlocks,
    BlockStatement? finallyBlock = null,
    SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        try
        {
            tryBlock.Run(manager);
        }
        catch (Old8Exception ex)
        {
            // 遍历所有catch块，查找匹配的异常类型
            foreach (var (exceptionType, exceptionVar, catchBlock) in catchBlocks)
            {
                // 如果异常类型为null，则匹配所有异常
                if (exceptionType == null ||
                    ex.GetType().Name == exceptionType ||
                    ex.GetType().BaseType?.Name == exceptionType)
                {
                    // 如果有异常变量，则将异常赋值给该变量
                    if (exceptionVar != null)
                    {
                        // 创建一个包含异常信息的值类型
                        manager.Set(exceptionVar, new ErrorValue(ex));
                    }

                    // 执行catch块
                    catchBlock.Run(manager);
                    return; // 只执行第一个匹配的catch块
                }
            }

            // 如果没有匹配的catch块，则重新抛出异常
            throw;
        }
        finally
        {
            // 执行finally块（如果存在）
            finallyBlock?.Run(manager);
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // IL生成暂不实现
    }

    public override OldStatement this[int index] => this;
    public override int Count => 0;
}