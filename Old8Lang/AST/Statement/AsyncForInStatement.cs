using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步 for-in 语句
/// 支持语法：async for item in asyncStream { ... }
/// 类似于 C# 的 await foreach
/// </summary>
public class AsyncForInStatement(
    LangId id,
    LangExpression expression,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    // 获取所有标识符，包括主标识符和附加标识符
    private List<LangId> AllIds { get; } = [id, .. (additionalIds ?? [])];

    /// <summary>
    /// 执行异步 for-in 循环
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public override void Run(VariateManager manager)
    {
        // 检查是否在生成器上下文中
        if (manager.GeneratorContext != null)
        {
            RunWithGeneratorContext(manager);
        }
        else
        {
            RunStandard(manager);
        }
    }

    /// <summary>
    /// 标准异步 for-in 循环（非生成器）
    /// </summary>
    private void RunStandard(VariateManager manager)
    {
        manager.AddChildren();
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            var value = expression.Run(manager);

            // 处理 TaskLangValue - await 任务并获取结果
            if (value is TaskLangValue taskValue)
            {
                // Await 任务获取结果
                value = taskValue.Await();
            }

            // 处理 AsyncStreamLangValue
            if (value is AsyncStreamLangValue asyncStream)
            {
                // 异步流迭代逻辑
                while (true)
                {
                    // 在每次循环迭代开始时重置控制流标志
                    manager.ControlFlowManager.ResetCurrentState();

                    // 运行异步流，获取下一个值（同步等待）
                    var nextValue = asyncStream.Run(manager);

                    // 检查异步流是否已完成
                    if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
                    {
                        break;
                    }

                    // 检查异步流是否处于Suspended状态，表示有值生成
                    if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
                    {
                        // 使用asyncStream.NextValue作为当前值
                        var currentValue = asyncStream.NextValue;

                        if (currentValue != null && !(currentValue is VoidLangValue))
                        {
                            // 赋值给标识符
                            if (AllIds.Count == 1)
                            {
                                manager.Set(id, currentValue);
                            }
                            else
                            {
                                // 多个标识符的情况，处理键值对
                                if (currentValue is TupleLangValue tupleValue)
                                {
                                    // 运行元组，获取实际值
                                    tupleValue.Run(manager);

                                    // 字典键值对，赋值给多个标识符
                                    var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                                    for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                                    {
                                        manager.Set(AllIds[i], values[i]);
                                    }
                                }
                                else
                                {
                                    // 不是键值对，只赋值给第一个标识符
                                    manager.Set(id, currentValue);
                                }
                            }

                            // 执行循环体
                            body.Run(manager);

                            // 处理break
                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                manager.ControlFlowManager.BreakFlag = false;
                                break;
                            }

                            // 处理continue
                            if (manager.ControlFlowManager.ContinueFlag)
                            {
                                manager.ControlFlowManager.ContinueFlag = false;
                                continue;
                            }
                        }
                    }
                }
            }
            // 处理异步生成器对象
            else if (value is AsyncGeneratorLangValue asyncGenerator)
            {
                // 异步生成器迭代逻辑
                while (true)
                {
                    // 在每次循环迭代开始时重置控制流标志
                    manager.ControlFlowManager.ResetCurrentState();

                    // 异步运行生成器，获取下一个值的 Task
                    var nextValueTask = asyncGenerator.RunAsync(manager);

                    // 等待 Task 完成并获取值
                    var nextValue = nextValueTask.Await();

                    // 检查生成器是否已完成
                    if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
                    {
                        break;
                    }

                    // 检查生成器是否处于Suspended状态，表示有值生成
                    if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
                    {
                        // 使用asyncGenerator.NextValue作为当前值
                        var currentValue = asyncGenerator.NextValue;

                        if (currentValue != null && !(currentValue is VoidLangValue))
                        {
                            // 赋值给标识符
                            if (AllIds.Count == 1)
                            {
                                manager.Set(id, currentValue);
                            }
                            else
                            {
                                // 多个标识符的情况，处理键值对
                                if (currentValue is TupleLangValue tupleValue)
                                {
                                    // 运行元组，获取实际值
                                    tupleValue.Run(manager);

                                    // 字典键值对，赋值给多个标识符
                                    var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                                    for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                                    {
                                        manager.Set(AllIds[i], values[i]);
                                    }
                                }
                                else
                                {
                                    // 不是键值对，只赋值给第一个标识符
                                    manager.Set(id, currentValue);
                                }
                            }

                            // 执行循环体
                            body.Run(manager);

                            // 处理break
                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            // 处理普通生成器对象（向后兼容）
            else if (value is GeneratorLangValue generator)
            {
                // 使用同步的 for-in 逻辑
                while (true)
                {
                    manager.ControlFlowManager.ResetCurrentState();

                    var nextValue = generator.Run(manager);

                    if (generator.State == GeneratorLangValue.GeneratorState.Completed)
                    {
                        break;
                    }

                    if (generator.State == GeneratorLangValue.GeneratorState.Suspended)
                    {
                        var currentValue = generator.NextValue;

                        if (currentValue != null && !(currentValue is VoidLangValue))
                        {
                            if (AllIds.Count == 1)
                            {
                                manager.Set(id, currentValue);
                            }
                            else
                            {
                                if (currentValue is TupleLangValue tupleValue)
                                {
                                    tupleValue.Run(manager);
                                    var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                                    for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                                    {
                                        manager.Set(AllIds[i], values[i]);
                                    }
                                }
                                else
                                {
                                    manager.Set(id, currentValue);
                                }
                            }

                            body.Run(manager);

                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            // 处理普通列表对象
            else if (value is ILangList oldList)
            {
                foreach (var idValue in oldList.GetItems())
                {
                    manager.ControlFlowManager.ResetCurrentState();

                    if (AllIds.Count == 1)
                    {
                        manager.Set(id, idValue);
                    }
                    else
                    {
                        if (idValue is TupleLangValue tupleValue)
                        {
                            tupleValue.Run(manager);
                            var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                            for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                            {
                                manager.Set(AllIds[i], values[i]);
                            }
                        }
                        else
                        {
                            manager.Set(id, idValue);
                        }
                    }

                    body.Run(manager);

                    if (manager.ControlFlowManager.BreakFlag)
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new TypeError(this, "ILangList、GeneratorLangValue、AsyncGeneratorLangValue、AsyncStreamLangValue 或 TaskLangValue", value.GetType().Name);
            }
        }
        finally
        {
            // 弹出当前控制流状态
            manager.ControlFlowManager.PopState();
            manager.RemoveChildren();
        }
    }

    /// <summary>
    /// 生成器上下文中的异步 for-in 循环
    /// </summary>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        // 不要在这里调用 AddChildren()！
        // 异步 for-in 循环的变量应该在外层作用域中
        manager.ControlFlowManager.PushState();

        var context = manager.GeneratorContext!;

        try
        {
            var value = expression.Run(manager);

            // 处理 TaskLangValue - await 任务并获取结果
            if (value is TaskLangValue taskValue)
            {
                value = taskValue.Await();
            }

            // 处理 AsyncStreamLangValue
            if (value is AsyncStreamLangValue asyncStream)
            {
                RunGeneratorContextAsyncStream(manager, asyncStream, context);
                return;
            }

            // 处理 AsyncGeneratorLangValue
            if (value is AsyncGeneratorLangValue asyncGenerator)
            {
                RunGeneratorContextAsyncGenerator(manager, asyncGenerator, context);
                return;
            }

            // 处理普通列表对象：需要支持从上次 yield 位置恢复
            if (value is ILangList oldList)
            {
                RunGeneratorContextList(manager, oldList, context);
                return;
            }

            throw new TypeError(this, "ILangList、GeneratorLangValue、AsyncGeneratorLangValue、AsyncStreamLangValue 或 TaskLangValue", value.GetType().Name);
        }
        finally
        {
            manager.ControlFlowManager.PopState();
        }
    }

    /// <summary>
    /// 在生成器上下文中迭代异步流
    /// 注意：不创建子作用域，循环变量在当前作用域中设置
    /// </summary>
    private void RunGeneratorContextAsyncStream(VariateManager manager, AsyncStreamLangValue asyncStream, Old8Lang.Generators.GeneratorExecutionContext context)
    {
        // 使用计数器跟踪迭代位置
        int iterationIndex = 0;
        bool wasResumingFromYield = false;

        // 检查是否从 yield 恢复（需要先处理可能的 "loop" 标记）
        if (context.ExecutionStack.Count > 0)
        {
            var topFrame = context.ExecutionStack.Peek();
            if (topFrame.BlockId == "loop")
            {
                context.ExecutionStack.Pop();
            }
        }

        if (context.ExecutionStack.Count > 0)
        {
            var frame = context.ExecutionStack.Peek();
            if (frame.BlockId == "async_for_in_stream_position" && frame.LoopIteration.HasValue)
            {
                context.ExecutionStack.Pop();
                iterationIndex = frame.LoopIteration.Value;
                wasResumingFromYield = true;
            }
        }

        // 关键：暂时清除 GeneratorContext，让内层异步流在非生成器模式下运行
        var outerContext = manager.GeneratorContext;
        try
        {
            manager.GeneratorContext = null;

            // 如果从 yield 恢复，先执行循环体的剩余部分，然后再获取下一个值
            if (wasResumingFromYield)
            {
                // 恢复 GeneratorContext 以执行循环体
                manager.GeneratorContext = outerContext;

                // 执行循环体的剩余部分
                body.Run(manager);

                // 检查是否再次 yield
                if (context.HasYielded)
                {
                    context.ExecutionStack.Push(new Old8Lang.Generators.GeneratorExecutionContext.BlockExecutionFrame
                    {
                        StatementIndex = -1,
                        BlockId = "async_for_in_stream_position",
                        LoopIteration = iterationIndex
                    });
                    return;
                }

                // 重置状态
                context.CurrentStatementIndex = 0;
                context.IsCompleted = false;

                // 处理 break
                if (manager.ControlFlowManager.BreakFlag)
                {
                    manager.ControlFlowManager.BreakFlag = false;
                    return;
                }

                // 处理 continue
                if (manager.ControlFlowManager.ContinueFlag)
                {
                    manager.ControlFlowManager.ContinueFlag = false;
                }

                // 循环体执行完毕，继续下一次迭代
                iterationIndex++;
                wasResumingFromYield = false;

                // 清除 GeneratorContext 以继续迭代
                manager.GeneratorContext = null;
            }

            while (true)
            {
                manager.ControlFlowManager.ResetCurrentState();

                // 运行异步流，获取下一个值
                var nextValue = asyncStream.Run(manager);

                // 检查异步流是否已完成
                if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
                {
                    break;
                }

                // 检查异步流是否处于Suspended状态
                if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
                {
                    var currentValue = asyncStream.NextValue;

                    if (currentValue != null && !(currentValue is VoidLangValue))
                    {
                        // 恢复外层GeneratorContext，准备执行循环体
                        manager.GeneratorContext = outerContext;

                        try
                        {
                            // 设置循环变量
                            if (AllIds.Count == 1)
                            {
                                manager.Set(id, currentValue);
                            }
                            else
                            {
                                // 多个标识符的情况，处理键值对
                                if (currentValue is TupleLangValue tupleValue)
                                {
                                    tupleValue.Run(manager);
                                    var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                                    for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                                    {
                                        manager.Set(AllIds[i], values[i]);
                                    }
                                }
                                else
                                {
                                    manager.Set(id, currentValue);
                                }
                            }

                            // 弹出可能存在的 "loop" 标记
                            if (context.ExecutionStack.Count > 0 && context.ExecutionStack.Peek().BlockId == "loop")
                            {
                                context.ExecutionStack.Pop();
                            }

                            // 重置 CurrentStatementIndex，开始执行循环体
                            context.CurrentStatementIndex = 0;

                            // 执行循环体
                            body.Run(manager);

                            // 检查是否遇到 yield
                            if (context.HasYielded)
                            {
                                // 保存当前迭代索引
                                context.ExecutionStack.Push(new Old8Lang.Generators.GeneratorExecutionContext.BlockExecutionFrame
                                {
                                    StatementIndex = -1,
                                    BlockId = "async_for_in_stream_position",
                                    LoopIteration = iterationIndex
                                });
                                return;
                            }

                            // 重置状态，准备下一次迭代
                            context.CurrentStatementIndex = 0;
                            context.IsCompleted = false;

                            // 处理 break
                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                manager.ControlFlowManager.BreakFlag = false;
                                break;
                            }

                            // 处理 continue
                            if (manager.ControlFlowManager.ContinueFlag)
                            {
                                manager.ControlFlowManager.ContinueFlag = false;
                                continue;
                            }

                            iterationIndex++;
                        }
                        finally
                        {
                            // 清除GeneratorContext，继续迭代内层流
                            manager.GeneratorContext = null;
                        }
                    }
                }
            }
        }
        finally
        {
            // 恢复外层的 GeneratorContext
            manager.GeneratorContext = outerContext;
        }
    }

    /// <summary>
    /// 在生成器上下文中迭代异步生成器
    /// </summary>
    private void RunGeneratorContextAsyncGenerator(VariateManager manager, AsyncGeneratorLangValue asyncGenerator, Old8Lang.Generators.GeneratorExecutionContext context)
    {
        // 类似 AsyncStream 的逻辑
        int iterationIndex = 0;
        bool wasResumingFromYield = false;

        if (context.ExecutionStack.Count > 0)
        {
            var topFrame = context.ExecutionStack.Peek();
            if (topFrame.BlockId == "async_for_in_generator_position" && topFrame.LoopIteration.HasValue)
            {
                context.ExecutionStack.Pop();
                iterationIndex = topFrame.LoopIteration.Value;
                wasResumingFromYield = true;
            }
        }

        // 保存外层的 GeneratorContext，暂时清除以避免嵌套冲突
        var outerContext = manager.GeneratorContext;
        try
        {
            // 内层异步生成器应该在非生成器模式下运行
            manager.GeneratorContext = null;

            while (true)
            {
                manager.ControlFlowManager.ResetCurrentState();

                var nextValueTask = asyncGenerator.RunAsync(manager);
                var nextValue = nextValueTask.Await();

                if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
                {
                    break;
                }

                if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
                {
                    var currentValue = asyncGenerator.NextValue;

                    if (currentValue != null && !(currentValue is VoidLangValue))
                    {
                        // 恢复外层的 GeneratorContext，以便执行循环体
                        manager.GeneratorContext = outerContext;

                        try
                        {
                            bool resumingFromYield = wasResumingFromYield && (iterationIndex == 0);
                            bool shouldSetVariable = (context.CurrentStatementIndex == 0);

                            if (shouldSetVariable)
                            {
                                manager.Set(id, currentValue);
                            }

                            if (!resumingFromYield)
                            {
                                context.CurrentStatementIndex = 0;
                            }

                            body.Run(manager);

                            if (context.HasYielded)
                            {
                                context.ExecutionStack.Push(new Old8Lang.Generators.GeneratorExecutionContext.BlockExecutionFrame
                                {
                                    StatementIndex = -1,
                                    BlockId = "async_for_in_generator_position",
                                    LoopIteration = iterationIndex
                                });
                                return;
                            }

                            context.CurrentStatementIndex = 0;
                            context.IsCompleted = false;
                            wasResumingFromYield = false;

                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                manager.ControlFlowManager.BreakFlag = false;
                                break;
                            }

                            if (manager.ControlFlowManager.ContinueFlag)
                            {
                                manager.ControlFlowManager.ContinueFlag = false;
                                continue;
                            }

                            iterationIndex++;
                        }
                        finally
                        {
                            // 清除 GeneratorContext 以继续迭代内层生成器
                            manager.GeneratorContext = null;
                        }
                    }
                }
            }
        }
        finally
        {
            // 恢复外层的 GeneratorContext
            manager.GeneratorContext = outerContext;
        }
    }

    /// <summary>
    /// 在生成器上下文中迭代普通列表
    /// </summary>
    private void RunGeneratorContextList(VariateManager manager, ILangList oldList, Old8Lang.Generators.GeneratorExecutionContext context)
    {
        var items = oldList.GetItems().ToList();
        int startIndex = 0;
        bool wasResumingFromYield = false;

        // 检查是否从 yield 恢复
        if (context.ExecutionStack.Count > 0)
        {
            var topFrame = context.ExecutionStack.Peek();
            if (topFrame.BlockId == "loop")
            {
                context.ExecutionStack.Pop();
            }
        }

        if (context.ExecutionStack.Count > 0)
        {
            var frame = context.ExecutionStack.Peek();
            if (frame.BlockId == "async_for_in_loop_position" && frame.LoopIteration.HasValue)
            {
                context.ExecutionStack.Pop();
                startIndex = frame.LoopIteration.Value;
                wasResumingFromYield = true;
            }
        }

        for (int i = startIndex; i < items.Count; i++)
        {
            bool resumingFromYield = wasResumingFromYield && (i == startIndex);
            manager.ControlFlowManager.ResetCurrentState();

            var idValue = items[i];
            bool shouldSetVariable = (context.CurrentStatementIndex == 0);

            if (shouldSetVariable)
            {
                if (AllIds.Count == 1)
                {
                    manager.Set(id, idValue);
                }
                else
                {
                    if (idValue is TupleLangValue tupleValue)
                    {
                        tupleValue.Run(manager);
                        var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };
                        for (var j = 0; j < AllIds.Count && j < values.Count; j++)
                        {
                            manager.Set(AllIds[j], values[j]);
                        }
                    }
                    else
                    {
                        manager.Set(id, idValue);
                    }
                }
            }

            // 弹出可能存在的 "loop" 标记
            if (context.ExecutionStack.Count > 0 && context.ExecutionStack.Peek().BlockId == "loop")
            {
                context.ExecutionStack.Pop();
            }

            if (!resumingFromYield)
            {
                context.CurrentStatementIndex = 0;
            }

            body.Run(manager);

            if (context.HasYielded)
            {
                context.ExecutionStack.Push(new Old8Lang.Generators.GeneratorExecutionContext.BlockExecutionFrame
                {
                    StatementIndex = -1,
                    BlockId = "async_for_in_loop_position",
                    LoopIteration = i
                });
                return;
            }

            context.CurrentStatementIndex = 0;
            context.IsCompleted = false;

            if (manager.ControlFlowManager.BreakFlag)
            {
                manager.ControlFlowManager.BreakFlag = false;
                break;
            }

            if (manager.ControlFlowManager.ContinueFlag)
            {
                manager.ControlFlowManager.ContinueFlag = false;
                continue;
            }
        }
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var ty = expression.OutputType(local) ?? typeof(object);

        // 对于字典类型，使用特殊处理
        if (ty == typeof(Dictionary<object, object>))
        {
            GenerateDictionaryIl(ilGenerator, local);
            return;
        }

        // 非字典类型，使用普通的IEnumerator处理
        var enumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        var current = ilGenerator.DeclareLocal(typeof(object));

        // 获取枚举器
        var getEnumeratorMethod = typeof(IEnumerable).GetMethod("GetEnumerator")!;
        expression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, enumerator);

        // 定义循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;

        // 循环开始
        ilGenerator.MarkLabel(loopStart);

        // 调用MoveNext
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素
        var currentProperty = typeof(IEnumerator).GetProperty("Current")!;
        var getCurrentMethod = currentProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);
        ilGenerator.Emit(OpCodes.Stloc, current);

        // 处理标识符赋值
        if (AllIds.Count == 1)
        {
            // 单个标识符，直接赋值
            local.AddLocalVar(AllIds[0].IdName, current);
        }
        else
        {
            // 多个标识符，只赋值给第一个
            local.AddLocalVar(AllIds[0].IdName, current);
        }

        // 生成循环体
        body.GenerateIl(ilGenerator, local);

        // 继续标签
        ilGenerator.MarkLabel(continueLabel);

        // 跳回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }

    /// <summary>
    /// 生成字典类型的IL代码
    /// </summary>
    private void GenerateDictionaryIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 保存字典到局部变量
        expression.LoadIlValue(ilGenerator, local);
        var dictLocal = ilGenerator.DeclareLocal(typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Stloc, dictLocal);

        // 获取字典的Keys集合
        var keysProperty = typeof(Dictionary<object, object>).GetProperty("Keys")!;
        var keysGetMethod = keysProperty.GetGetMethod()!;

        // 获取Keys集合的IEnumerable接口
        var enumerableType = typeof(IEnumerable);
        ilGenerator.Emit(OpCodes.Ldloc, dictLocal);
        ilGenerator.Emit(OpCodes.Callvirt, keysGetMethod);

        // 获取Keys集合的枚举器
        var keysEnumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        var keysGetEnumeratorMethod = enumerableType.GetMethod("GetEnumerator")!;
        ilGenerator.Emit(OpCodes.Callvirt, keysGetEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, keysEnumerator);

        // 定义循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;

        // 循环开始
        ilGenerator.MarkLabel(loopStart);

        // 调用MoveNext
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
        ilGenerator.Emit(OpCodes.Ldloc, keysEnumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前键
        var currentProperty = typeof(IEnumerator).GetProperty("Current")!;
        var getCurrentMethod = currentProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Ldloc, keysEnumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);
        var keyLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, keyLocal);

        // 将键添加到局部变量管理器
        local.AddLocalVar(AllIds[0].IdName, keyLocal);

        // 如果有多个标识符（键值对遍历），获取值
        if (AllIds.Count > 1)
        {
            // 获取字典的索引器方法
            var itemProperty = typeof(Dictionary<object, object>).GetProperty("Item")!;
            var getItemMethod = itemProperty.GetGetMethod()!;

            // 加载字典和键，调用索引器获取值
            ilGenerator.Emit(OpCodes.Ldloc, dictLocal);
            ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
            ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);

            // 保存值到局部变量
            var valueLocal = ilGenerator.DeclareLocal(typeof(object));
            ilGenerator.Emit(OpCodes.Stloc, valueLocal);

            // 将值添加到局部变量管理器
            local.AddLocalVar(AllIds[1].IdName, valueLocal);
        }

        // 生成循环体
        body.GenerateIl(ilGenerator, local);

        // 继续标签
        ilGenerator.MarkLabel(continueLabel);

        // 跳回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }

    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;
}
