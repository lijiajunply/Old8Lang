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
                    var nextValue = generator.Run(manager);

                    // 检查生成器是否已完成
                    if (generator.State == GeneratorLangValue.GeneratorState.Completed)
                    {
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
        // 注意：在生成器上下文中不调用 AddChildren()！
        // 生成器需要跨 yield 保持所有变量状态，子作用域机制不适用
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
                int? savedStatementIndex = null;  // 保存循环语句在父 BlockStatement 中的索引
                bool wasResumingFromYield = false;

                // 检查是否从 yield 恢复
                // BlockStatement 可能会压入一个 "loop" 标志，需要先弹出并保存其 StatementIndex
                if (context.ExecutionStack.Count > 0)
                {
                    var topFrame = context.ExecutionStack.Peek();
                    if (topFrame.BlockId == "loop")
                    {
                        var loopFrame = context.ExecutionStack.Pop();
                        savedStatementIndex = loopFrame.StatementIndex;
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

                        // 如果帧中有 StatementIndex，使用它
                        if (frame.StatementIndex >= 0)
                        {
                            savedStatementIndex = frame.StatementIndex;
                        }
                    }
                }

                // 从 startIndex 开始迭代
                //  - 如果是首次运行,从 0 开始
                //  - 如果从 yield 恢复,从保存的索引开始
                for (int i = startIndex; i < items.Count; i++)
                {
                    manager.ControlFlowManager.ResetCurrentState();

                    var idValue = items[i];

                    // 设置循环变量
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

                    // 执行循环体前，弹出可能存在的 "loop" 标记并保存 StatementIndex
                    if (context.ExecutionStack.Count > 0 && context.ExecutionStack.Peek().BlockId == "loop")
                    {
                        var loopFrame = context.ExecutionStack.Pop();
                        // 更新 savedStatementIndex（用于下次 yield 保存）
                        savedStatementIndex = loopFrame.StatementIndex;
                    }

                    // 重置 CurrentStatementIndex，从头开始执行循环体
                    context.CurrentStatementIndex = 0;

                    // 执行循环体
                    body.Run(manager);

                    // 检查是否遇到 yield
                    if (context.HasYielded)
                    {
                        // 检查是否是嵌套 yield（栈中有其他循环的标记）
                        bool isNestedYield = false;
                        if (context.ExecutionStack.Count > 0)
                        {
                            var topFrame = context.ExecutionStack.Peek();
                            if (topFrame.BlockId == "for_in_loop_position")
                            {
                                isNestedYield = true;
                            }
                        }

                        // 决定保存哪个索引:
                        //  - 如果是嵌套 yield (内层循环 yield 了),外层保存当前索引(继续当前迭代的剩余部分)
                        //  - 如果是直接 yield (本循环直接 yield),保存下一个索引(进入下一次迭代)
                        int nextIndex = isNestedYield ? i : (i + 1);
                        context.ExecutionStack.Push(new GeneratorExecutionContext.BlockExecutionFrame
                        {
                            StatementIndex = savedStatementIndex ?? -1,  // 保存语句索引
                            BlockId = "for_in_loop_position",
                            LoopIteration = nextIndex
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
            // 注意：不调用 RemoveChildren()，因为没有调用 AddChildren()
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