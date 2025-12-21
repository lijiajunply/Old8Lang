using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Generators;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public class ForInStatement(
    LangId id,
    LangExpression expression,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    // 获取所有标识符，包括主标识符和附加标识符
    private List<LangId> AllIds
    {
        get => [id, .. field];
    } = additionalIds ?? [];

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
    /// 标准 for-in 循环（非生成器）
    /// </summary>
    private void RunStandard(VariateManager manager)
    {
        manager.AddChildren();
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            var value = expression.Run(manager);

            // 处理异步生成器对象（异步流）
            if (value is AsyncGeneratorLangValue asyncGenerator)
            {
                // 异步生成器迭代逻辑
                while (true)
                {
                    // 在每次循环迭代开始时重置控制流标志
                    manager.ControlFlowManager.ResetCurrentState();

                    // 运行异步生成器，获取下一个值（同步等待）
                    var nextValue = asyncGenerator.Run(manager);

                    // 检查异步生成器是否已完成
                    if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
                    {
                        break;
                    }

                    // 检查异步生成器是否处于Suspended状态，表示有值生成
                    if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
                    {
                        // 使用asyncGenerator.NextValue作为当前值
                        var currentValue = asyncGenerator.NextValue;

                        if (currentValue != null && currentValue is not VoidLangValue)
                        {
                            // 赋值给标识符
                            manager.Set(id, currentValue);

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
            // 处理AsyncStreamLangValue（也是异步生成器的包装）
            else if (value is AsyncStreamLangValue asyncStream)
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

                        if (currentValue != null && currentValue is not VoidLangValue)
                        {
                            // 赋值给标识符
                            manager.Set(id, currentValue);

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
            // 处理生成器对象
            else if (value is GeneratorLangValue generator)
            {
                // 生成器迭代逻辑
                while (true)
                {
                     // 在每次循环迭代开始时重置控制流标志
                    manager.ControlFlowManager.ResetCurrentState();

                    // 运行生成器，获取下一个值
                    System.Console.WriteLine($"[DEBUG OUTER] Calling generator.Run()");
                    var nextValue = generator.Run(manager);
                    System.Console.WriteLine($"[DEBUG OUTER] generator.State={generator.State}, NextValue={generator.NextValue}");

                    // 检查生成器是否已完成
                    if (generator.State == GeneratorLangValue.GeneratorState.Completed)
                    {
                        System.Console.WriteLine($"[DEBUG OUTER] Generator completed, breaking");
                        break;
                    }

                    // 检查生成器是否处于Suspended状态，表示有值生成
                    if (generator.State == GeneratorLangValue.GeneratorState.Suspended)
                    {
                        // 使用generator.NextValue作为当前值
                        var currentValue = generator.NextValue;

                        if (currentValue != null && currentValue is not VoidLangValue)
                        {
                            // 赋值给标识符
                            // 多个标识符的情况，只赋值给第一个
                            manager.Set(id, currentValue);

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
                        else
                        {
                            System.Console.WriteLine($"[DEBUG OUTER] currentValue is null or VoidLangValue");
                        }
                    }
                }
            }
            // 处理普通列表对象
            else if (value is ILangList oldList)
            {
                foreach (var idValue in oldList.GetItems())
                {
                    if (AllIds.Count == 1)
                    {
                        // 单个标识符的情况，保持原有行为
                        manager.Set(id, idValue);
                    }
                    else
                    {
                        // 多个标识符的情况，处理键值对
                        if (idValue is TupleLangValue tupleValue)
                        {
                            // 运行元组，获取实际值
                            tupleValue.Run(manager);

                            // 字典键值对，赋值给多个标识符
                            var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                            for (var i = 0; i < AllIds.Count && i < values.Count; i++)
                            {
                                manager.Set(AllIds[i], values[i]);
                            }
                        }
                        else
                        {
                            // 不是键值对，只赋值给第一个标识符
                            manager.Set(id, idValue);
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
            else
            {
                throw new TypeError(this, "IOldList或GeneratorLangValue", value.GetType().Name);
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
    /// 生成器上下文中的 for-in 循环
    /// </summary>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        // 注意：不要在这里调用 AddChildren()！
        // for-in 循环的变量应该在外层作用域中，否则从 yield 恢复时会创建新的子作用域导致变量丢失
        manager.ControlFlowManager.PushState();

        var context = manager.GeneratorContext!;

        try
        {
            var value = expression.Run(manager);

            // 对于生成器和异步生成器，保持原有逻辑（因为它们自己管理状态）
            if (value is AsyncGeneratorLangValue asyncGenerator)
            {
                RunStandardAsyncGenerator(manager, asyncGenerator);
                return;
            }

            if (value is AsyncStreamLangValue asyncStream)
            {
                RunStandardAsyncStream(manager, asyncStream);
                return;
            }

            if (value is GeneratorLangValue generator)
            {
                RunStandardGenerator(manager, generator);
                return;
            }

            // 处理普通列表：需要支持从上次 yield 位置恢复
            if (value is ILangList oldList)
            {
                var items = oldList.GetItems().ToList();
                int startIndex = 0;
                bool wasResumingFromYield = false;  // 标记最初是否从 yield 恢复

                // 检查是否从 yield 恢复
                // BlockStatement 可能会压入一个 "loop" 标志，需要先弹出
                if (context.ExecutionStack.Count > 0)
                {
                    var topFrame = context.ExecutionStack.Peek();
                    if (topFrame.BlockId == "loop")
                    {
                        context.ExecutionStack.Pop();
                    }
                }

                // 然后检查是否有保存的循环位置
                if (context.ExecutionStack.Count > 0)
                {
                    var frame = context.ExecutionStack.Peek();
                    if (frame.BlockId == "for_in_loop_position" && frame.LoopIteration.HasValue)
                    {
                        context.ExecutionStack.Pop();
                        startIndex = frame.LoopIteration.Value;
                        wasResumingFromYield = true;
                        System.Console.WriteLine($"[DEBUG] Resuming from yield, startIndex={startIndex}");
                    }
                }

                // 从 startIndex 开始迭代
                // 注意：这里使用 for 循环，每次迭代后检查 HasYielded
                // 如果遇到 yield，保存位置并返回；否则继续下一次迭代
                for (int i = startIndex; i < items.Count; i++)
                {
                    // 判断当前迭代是否是从 yield 恢复的
                    // 只有第一次迭代(i == startIndex)且最初从 yield 恢复时才是 true
                    bool resumingFromYield = wasResumingFromYield && (i == startIndex);
                    System.Console.WriteLine($"[DEBUG] ForInStatement: iteration i={i}, startIndex={startIndex}, resumingFromYield={resumingFromYield}");
                    manager.ControlFlowManager.ResetCurrentState();

                    var idValue = items[i];

                    // 检查是否从 yield 恢复（通过 CurrentStatementIndex 判断）
                    // 如果 CurrentStatementIndex > 0，说明循环体正在执行中（从 yield 恢复）
                    // 此时不应该重新设置循环变量
                    bool shouldSetVariable = (context.CurrentStatementIndex == 0);
                    System.Console.WriteLine($"[DEBUG] CurrentStatementIndex={context.CurrentStatementIndex}, shouldSetVariable={shouldSetVariable}");

                    if (shouldSetVariable)
                    {
                        if (AllIds.Count == 1)
                        {
                            manager.Set(id, idValue);
                            System.Console.WriteLine($"[DEBUG] Set {id.IdName} = {idValue}");
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
                    else
                    {
                        System.Console.WriteLine($"[DEBUG] Skipping Set (CurrentStatementIndex > 0, resuming from yield)");
                    }

                    // 执行循环体前，弹出可能存在的 "loop" 标记
                    // 这是外层 BlockStatement 在上一次 yield 时压入的
                    if (context.ExecutionStack.Count > 0 && context.ExecutionStack.Peek().BlockId == "loop")
                    {
                        context.ExecutionStack.Pop();
                        System.Console.WriteLine($"[DEBUG] Popped 'loop' marker before body.Run()");
                    }

                    // 如果不是从 yield 恢复，重置 CurrentStatementIndex
                    // 这样循环体就会从头开始执行
                    if (!resumingFromYield)
                    {
                        context.CurrentStatementIndex = 0;
                        System.Console.WriteLine($"[DEBUG] Reset CurrentStatementIndex to 0 for new iteration");
                    }

                    // 执行循环体
                    System.Console.WriteLine($"[DEBUG] About to run body for i={i}, body.Count={body.Count}");
                    body.Run(manager);
                    System.Console.WriteLine($"[DEBUG] After body.Run(), HasYielded={context.HasYielded}");

                    // 检查是否遇到 yield
                    if (context.HasYielded)
                    {
                        // 保存当前迭代的索引（i）
                        // BlockStatement 会负责从 yield 之后的语句继续执行
                        System.Console.WriteLine($"[DEBUG] Yielded at i={i}, saving i={i}");
                        context.ExecutionStack.Push(new GeneratorExecutionContext.BlockExecutionFrame
                        {
                            StatementIndex = -1,
                            BlockId = "for_in_loop_position",
                            LoopIteration = i  // 保存当前迭代的索引
                        });
                        return;
                    }

                    // 重置 CurrentStatementIndex 和 IsCompleted，准备下一次迭代
                    // 关键修复：在完成当前迭代后，必须重置这些标志
                    // 否则 BlockStatement 可能会因为 IsCompleted=true 而提前退出
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

                    // 没有 yield，继续下一次迭代
                }
            }
            else
            {
                throw new TypeError(this, "IOldList或GeneratorLangValue", value.GetType().Name);
            }
        }
        finally
        {
            manager.ControlFlowManager.PopState();
            // 注意：不要调用 RemoveChildren()，因为我们没有调用 AddChildren()
        }
    }

    /// <summary>
    /// 在生成器上下文中处理异步生成器（保持原有逻辑）
    /// </summary>
    private void RunStandardAsyncGenerator(VariateManager manager, AsyncGeneratorLangValue asyncGenerator)
    {
        while (true)
        {
            manager.ControlFlowManager.ResetCurrentState();
            var nextValue = asyncGenerator.Run(manager);

            if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
            {
                break;
            }

            if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
            {
                var currentValue = asyncGenerator.NextValue;

                if (currentValue != null && currentValue is not VoidLangValue)
                {
                    manager.Set(id, currentValue);
                    body.Run(manager);

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
        }
    }

    /// <summary>
    /// 在生成器上下文中处理异步流（保持原有逻辑）
    /// </summary>
    private void RunStandardAsyncStream(VariateManager manager, AsyncStreamLangValue asyncStream)
    {
        while (true)
        {
            manager.ControlFlowManager.ResetCurrentState();
            var nextValue = asyncStream.Run(manager);

            if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
            {
                break;
            }

            if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
            {
                var currentValue = asyncStream.NextValue;

                if (currentValue != null && currentValue is not VoidLangValue)
                {
                    manager.Set(id, currentValue);
                    body.Run(manager);

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
        }
    }

    /// <summary>
    /// 在生成器上下文中处理同步生成器（保持原有逻辑）
    /// </summary>
    private void RunStandardGenerator(VariateManager manager, GeneratorLangValue generator)
    {
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

                if (currentValue != null && currentValue is not VoidLangValue)
                {
                    manager.Set(id, currentValue);
                    body.Run(manager);

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
        }
    }

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
    /// 生成字典类型的IL代码，使用更简单可靠的方式
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