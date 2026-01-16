using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Expression.Value;

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
    public List<(string? exceptionType, LangId? exceptionVar, LangExpression? filter, BlockStatement catchBlock)> CatchBlocks => catchBlocks;

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
                return;
            }
        }
        catch (Old8Exception ex)
        {
            // 遍历所有catch块，查找匹配的异常类型
            foreach (var (exceptionType, exceptionVar, filter, catchBlock) in catchBlocks)
            {
                // 如果异常类型为null，则匹配所有异常
                if (exceptionType is null ||
                    IsMatch(ex, exceptionType))
                {
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
                        return;
                    }
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
                var (_, exceptionVar, _, catchBlock) = catchBlocks[0];

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
                        return;
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
                    return;
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
        // 检查如果有finally块，那么try块和catch块中不能包含return语句
        // 这是因为在.NET IL中，try块或catch块中的return语句与finally块一起使用会导致无效的IL代码
        // 注意：在异步状态机模式下，return 语句会编译为 Leave 指令，这是合法的，所以跳过检查
        if (finallyBlock is not null && local.AsyncStateMachineGenerator == null)
        {
            // 检查try块中是否包含return语句
            if (ContainsReturnStatement(tryBlock))
            {
                throw new CompilerException("当有finally块时，try块中不能包含return语句", Position);
            }

            // 检查所有catch块中是否包含return语句
            foreach (var (_, _, _, catchBlock) in catchBlocks)
            {
                if (ContainsReturnStatement(catchBlock))
                {
                    throw new CompilerException("当有finally块时，catch块中不能包含return语句", Position);
                }
            }
        }

        // 开始异常处理块
        ilGenerator.BeginExceptionBlock();

        // 生成try块的IL代码
        tryBlock.GenerateIl(ilGenerator, local);

        // 生成catch块的IL代码
        foreach (var (exceptionType, exceptionVar, filter, catchBlock) in catchBlocks)
        {
            // 开始catch块，捕获所有类型的异常
            // TODO: 支持过滤器 (System.Reflection.Emit 对于过滤器支持有限，这里暂时忽略过滤器)
            ilGenerator.BeginCatchBlock(typeof(Exception));

            // 如果有异常变量，将其添加到局部变量管理器
            if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
            {
                // 将异常对象包装为ExceptionWrapper,以便ToString()只返回消息
                // 栈顶是捕获到的Exception对象
                // 调用 new ExceptionWrapper(exception)
                ilGenerator.Emit(OpCodes.Newobj, typeof(Old8Lang.Compiler.ExceptionWrapper).GetConstructor([typeof(Exception)])!);

                var exceptionLocal = ilGenerator.DeclareLocal(typeof(Old8Lang.Compiler.ExceptionWrapper));
                ilGenerator.Emit(OpCodes.Stloc, exceptionLocal);

                // 将异常变量添加到局部变量管理器
                local.AddLocalVar(exceptionVar.IdName, exceptionLocal);
            }
            else
            {
                // 如果没有异常变量，清空堆栈
                ilGenerator.Emit(OpCodes.Pop);
            }

            // 生成catch块的IL代码
            catchBlock.GenerateIl(ilGenerator, local);

            // 如果添加了异常变量，移除它
            if (exceptionVar is not null && !string.IsNullOrEmpty(exceptionVar.IdName))
            {
                local.RemoveLocalVar(exceptionVar.IdName);
            }
        }

        // 如果有finally块，生成finally块的IL代码
        if (finallyBlock is not null)
        {
            // 开始finally块
            ilGenerator.BeginFinallyBlock();

            // 设置在finally块中的标志
            local.IsInFinallyBlock = true;

            // 生成finally块的IL代码
            finallyBlock.GenerateIl(ilGenerator, local);

            // 恢复标志
            local.IsInFinallyBlock = false;
        }

        // 结束异常处理块
        ilGenerator.EndExceptionBlock();
    }

    /// <summary>
    /// 检查try块、catch块和finally块中是否包含return语句
    /// </summary>
    /// <param name="statement">要检查的语句</param>
    /// <returns>如果包含return语句，返回true；否则返回false</returns>
    private bool ContainsReturnStatement(OldStatement statement)
    {
        if (statement is ReturnStatement)
        {
            return true;
        }

        for (int i = 0; i < statement.Count; i++)
        {
            var child = statement[i];
            if (child is null) continue;
            if (ContainsReturnStatement(child))
            {
                return true;
            }
        }

        return false;
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