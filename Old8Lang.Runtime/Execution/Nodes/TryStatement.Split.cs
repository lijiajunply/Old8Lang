using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// TryStatement 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class TryStatement
{
    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    internal void RunWithGeneratorContext(VariateManager manager)
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
                    catchBlock.Accept(new InterpreterVisitor(manager));

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
                tryBlock.Accept(new InterpreterVisitor(manager));

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

                                var filterResult = filter.Accept(new InterpreterVisitor(manager));
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
                            catchBlock.Accept(new InterpreterVisitor(manager));

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
    /// <summary>
    /// 标准模式执行（非生成器）
    /// </summary>
    internal void RunStandard(VariateManager manager)
    {
        try
        {
            tryBlock.Accept(new InterpreterVisitor(manager));
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

                    var filterResult = filter.Accept(new InterpreterVisitor(manager));
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
                catchBlock.Accept(new InterpreterVisitor(manager));
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
            finallyBlock?.Accept(new InterpreterVisitor(manager));
        }
    }
}
