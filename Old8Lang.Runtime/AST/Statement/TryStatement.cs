using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Try语句，用于异常处理
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

    public override void Run(VariateManager manager)
    {
        // 检查是否在生成器上下文中
        if (manager.GeneratorContext is not null)
        {
            RunWithGeneratorContext(manager);
        }
        else
        {
            RunStandard(manager);
        }
    }

    /// <summary>
    /// 标准模式执行（非生成器）
    /// </summary>
    private void RunStandard(VariateManager manager)
    {
        try
        {
            tryBlock.Run(manager);
            // 检查try块是否执行了return语句
            if (manager.IsReturn)
            {
            }
        }
        catch (Old8Exception ex)
        {
            // 遍历所有catch块，查找匹配的异常类型
            foreach (var (exceptionType, exceptionVar, filter, catchBlock) in catchBlocks)
            {
                // 如果异常类型为null，则匹配所有异常
                if (exceptionType is not null &&
                    !IsMatch(ex, exceptionType)) continue;
                // 检查过滤器
                if (filter != null)
                {
                    // 需要先将异常赋值给变量，以便过滤器可以使用它
                    // 注意：这里需要保存之前的变量状态，以便在过滤器不匹配时恢复
                    // 但为了简单起见，我们假设变量名不会冲突，或者接受副作用

                    if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
                    {
                        manager.Set(exceptionVar, new ErrorLangValue(ex));
                    }
                    else
                    {
                        var defaultExceptionVar = new LangId("exception");
                        manager.Set(defaultExceptionVar, new ErrorLangValue(ex));
                    }

                    var filterResult = filter.Run(manager);
                    if (filterResult is not BoolLangValue boolValue || !boolValue.Value)
                    {
                        // 过滤器不匹配，继续下一个catch块
                        continue;
                    }
                }
                else
                {
                    // 没有过滤器，直接赋值变量
                    if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
                    {
                        manager.Set(exceptionVar, new ErrorLangValue(ex));
                    }
                    else
                    {
                        var defaultExceptionVar = new LangId("exception");
                        manager.Set(defaultExceptionVar, new ErrorLangValue(ex));
                    }
                }

                // 执行catch块
                catchBlock.Run(manager);
                // 检查catch块是否执行了return语句
                if (manager.IsReturn)
                {
                }

                return; // 只执行第一个匹配的catch块
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

    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 检查是否从 catch 块恢复
        bool isResumingFromCatch = !string.IsNullOrEmpty(context.ExecutionPath) &&
                                   context.ExecutionPath.Contains("/catch");

        // 如果从 catch 块恢复，直接执行 catch 块，跳过 try 块
        if (isResumingFromCatch)
        {
            // 执行第一个 catch 块（简化处理，假设只有一个 catch）
            if (catchBlocks.Count > 0)
            {
                var (_, _, _, catchBlock) = catchBlocks[0];

                // 压入 catch 路径
                context.PathStack.Push("/catch");
                try
                {
                    catchBlock.Run(manager);

                    // 检查是否 yield
                    if (context.HasYielded)
                    {
                        return;
                    }

                    // 检查是否 return
                    if (manager.IsReturn)
                    {
                    }
                }
                finally
                {
                    context.PathStack.Pop();
                }
            }
        }
        else
        {
            // 正常���行 try 块
            context.PathStack.Push("/try");
            try
            {
                tryBlock.Run(manager);

                // 检查是否 yield
                if (context.HasYielded)
                {
                    return;
                }

                // 检查try块是否执行了return语句
                if (manager.IsReturn)
                {
                }
            }
            catch (Old8Exception ex)
            {
                // 弹出 try 路径，压入 catch 路径
                context.PathStack.Pop();
                context.PathStack.Push("/catch");

                try
                {
                    // 遍历所有catch块，查找匹配的异常类型
                    foreach (var (exceptionType, exceptionVar, filter, catchBlock) in catchBlocks)
                    {
                        // 如果异常类型为null，则匹配所有异常
                        if (exceptionType is null || IsMatch(ex, exceptionType))
                        {
                            // 检查过滤器
                            if (filter != null)
                            {
                                if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
                                {
                                    manager.Set(exceptionVar, new ErrorLangValue(ex));
                                }
                                else
                                {
                                    var defaultExceptionVar = new LangId("exception");
                                    manager.Set(defaultExceptionVar, new ErrorLangValue(ex));
                                }

                                var filterResult = filter.Run(manager);
                                if (filterResult is not BoolLangValue boolValue || !boolValue.Value)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
                                {
                                    manager.Set(exceptionVar, new ErrorLangValue(ex));
                                }
                                else
                                {
                                    var defaultExceptionVar = new LangId("exception");
                                    manager.Set(defaultExceptionVar, new ErrorLangValue(ex));
                                }
                            }

                            // 执行catch块
                            catchBlock.Run(manager);

                            // 检查是否 yield
                            if (context.HasYielded)
                            {
                                return;
                            }

                            // 检查catch块是否执行了return语句
                            if (manager.IsReturn)
                            {
                                return;
                            }

                            // 弹出 catch 路径
                            context.PathStack.Pop();
                            return; // 只执行第一个匹配的catch块
                        }
                    }

                    // 如果没有匹配的catch块，则重新抛出异常
                    throw;
                }
                finally
                {
                    // 确保 catch 路径被弹出（如果还没有弹出）
                    if (context.PathStack.Count > 0 && context.PathStack.Peek() == "/catch")
                    {
                        context.PathStack.Pop();
                    }
                }
            }
            finally
            {
                // 确保 try 路径被弹出（如果还没有弹出）
                if (context.PathStack.Count > 0 && context.PathStack.Peek() == "/try")
                {
                    context.PathStack.Pop();
                }
            }
        }

        // 注意：在生成器上下文中，finally 块不应该在 try 块执行过程中执行
        // finally 块应该由生成器框架在适当的时机调用（比如生成器完成或异常时）
        // 这里不执行 finally 块，因为：
        // 1. 如果 try 块中 yield 了，生成器会暂停，不应该执行 finally
        // 2. finally 块需要在生成器真正完成时才执行
        // 目前的架构中，生成器状态机会负责在适当时机执行 finally 块
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 委托给 CompilerVisitor 处理，以支持更复杂的逻辑（如多重 catch 块分发）
        var visitor = new Visitor.CompilerVisitor(ilGenerator, local);
        Accept(visitor);
    }

    private static bool IsMatch(Old8Exception exception, string exceptionType)
    {
        if (string.IsNullOrEmpty(exceptionType) || exceptionType == "Exception" || exceptionType == "Old8Exception")
        {
            return true;
        }

        var currentType = exception.GetType();
        while (currentType is not null)
        {
            // 精确匹配类型名称
            if (currentType.Name == exceptionType)
            {
                return true;
            }

            // 精确匹配完整类型名称（包括命名空间）
            if (currentType.FullName == exceptionType)
            {
                return true;
            }

            // 检查完整命名空间路径中的类型（支持"Error.RuntimeError"格式）
            if (currentType.FullName?.EndsWith($".{exceptionType}") == true ||
                currentType.FullName?.Contains($".{exceptionType}.") == true)
            {
                return true;
            }

            // 移动到父类，支持异常继承关系匹配
            currentType = currentType.BaseType;
        }

        return false;
    }

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