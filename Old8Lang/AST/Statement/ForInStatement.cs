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

public partial class ForInStatement(
    LangId id,
    LangExpression expression,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    // 公共属性，用于外部访问
    public LangId Id => id;
    public LangExpression Expression => expression;
    public OldStatement Body => body;

    // 获取所有标识符，包括主标识符和附加标识符
    private List<LangId> AllIds
    {
        get => [id, .. field];
    } = additionalIds ?? [];

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
                    asyncGenerator.Run(manager);

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

                        if (currentValue is not null && currentValue is not VoidLangValue)
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
                    asyncStream.Run(manager);

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

                        if (currentValue is not null && currentValue is not VoidLangValue)
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
                    generator.Run(manager);

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

                        if (currentValue is not null && currentValue is not VoidLangValue)
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
                            }
                        }
                        else
                        {
                            // 如果返回 VoidLangValue，生成器已完成
                            break;
                        }
                    }
                    else
                    {
                        // 如果既不是Completed也不是Suspended，则退出循环
                        break;
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
                            var values = tupleValue.GetItems().ToList();

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
    /// 生成器上下文中的 for-in 循环（新架构）
    /// </summary>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        // 不要在这里调用 AddChildren()，生成器需要跨yield保持所有变量状态
        manager.ControlFlowManager.PushState();

        var context = manager.GeneratorContext!;

        try
        {
            // 检查是否有缓存的生成器（从 yield 恢复）
            {
                var loopPath = context.GetCurrentPath() + "/for-in";
                var cacheKey = loopPath + "/generator";
                if (context.AsyncStreamCache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is GeneratorLangValue cachedGenerator)
                {
                    // 从缓存恢复，直接使用缓存的生成器，不重新执行 expression
                    RunStandardGenerator(manager, cachedGenerator, context);
                    return;
                }
            }

            // 首次执行，运行表达式获取值
            var value = expression.Run(manager);

            // 对于生成器和异步生成器，保持原有逻辑（它们自己管理状态）
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
                RunStandardGenerator(manager, generator, context);
                return;
            }

            // 处理普通列表：需要支持从上次 yield 位置恢复
            if (value is ILangList oldList)
            {
                var items = oldList.GetItems().ToList();

                // 获取当前循环路径
                var loopPath = context.GetCurrentPath() + "/for-in";

                // 从循环状态中恢复起始索引
                int startIndex = 0;
                bool isResumingLoop = false;
                if (context.LoopStates.TryGetValue(loopPath, out var savedIndex))
                {
                    startIndex = savedIndex;
                    isResumingLoop = true;
                }

                // 将循环路径压栈
                context.PathStack.Push("/for-in");

                try
                {
                    // 从 startIndex 开始迭代
                    for (int i = startIndex; i < items.Count; i++)
                    {
                        manager.ControlFlowManager.ResetCurrentState();

                        // 检查是否有子循环状态存在
                        var childLoopPrefix = loopPath + "/";
                        var hasChildLoopState = context.LoopStates.Keys.Any(k => k.StartsWith(childLoopPrefix));

                        // 如果没有子循环状态，说明这是新的迭代，清除之前可能残留的子循环状态
                        // 如果有子循环状态，说明是从子循环恢复，保持状态
                        if (!hasChildLoopState)
                        {
                            var childKeysToRemove = context.LoopStates.Keys.Where(k => k.StartsWith(childLoopPrefix)).ToList();
                            foreach (var key in childKeysToRemove)
                            {
                                context.LoopStates.Remove(key);
                            }
                        }

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
                                var values = tupleValue.GetItems().ToList();
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

                        // 清除 ExecutionPath，让新的迭代能够完整执行
                        // 关键修复：只有在从当前迭代内部的 yield 恢复时才保持 ExecutionPath
                        // 如果 ExecutionPath 指向上一次迭代的语句，必须清除，否则会导致语句被错误跳过
                        bool isResumingFromCurrentIteration = isResumingLoop && i == startIndex && !string.IsNullOrEmpty(context.ExecutionPath);

                        if (!isResumingFromCurrentIteration)
                        {
                            // 新迭代或首次进入：清除 ExecutionPath，让所有语句正常执行
                            // 但如果有子循环状态，保持 ExecutionPath 以便子循环恢复
                            if (!hasChildLoopState)
                            {
                                context.ExecutionPath = "";
                            }
                        }
                        // else: 从当前迭代内部的 yield 恢复，保持 ExecutionPath

                        // 执行循环体
                        body.Run(manager);

                        // 检查是否遇到 yield
                        if (context.HasYielded)
                        {
                            // 保存当前迭代位置，以便从 yield 后继续执行
                            context.LoopStates[loopPath] = i;
                            return;
                        }

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
                        }
                    }

                    // 循环完成，清除当前循环及其所有子循环的状态
                    var keysToRemove = context.LoopStates.Keys.Where(k => k == loopPath || k.StartsWith(loopPath + "/")).ToList();
                    foreach (var key in keysToRemove)
                    {
                        context.LoopStates.Remove(key);
                    }

                    // 循环正常完成，清除 ExecutionPath，以便外层 BlockStatement 能继续执行后续语句
                    context.ExecutionPath = "";
                }
                finally
                {
                    // 弹出循环路径
                    context.PathStack.Pop();
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
            asyncGenerator.Run(manager);

            if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
            {
                break;
            }

            if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
            {
                var currentValue = asyncGenerator.NextValue;

                if (currentValue is not null && currentValue is not VoidLangValue)
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
            asyncStream.Run(manager);

            if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
            {
                break;
            }

            if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
            {
                var currentValue = asyncStream.NextValue;

                if (currentValue is not null && currentValue is not VoidLangValue)
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
                    }
                }
            }
        }
    }

    /// <summary>
    /// 在生成器上下文中处理同步生成器（新架构：支持嵌套生成器状态保存）
    /// </summary>
    private void RunStandardGenerator(VariateManager manager, GeneratorLangValue generatorValue, GeneratorExecutionContext context)
    {
        // 获取当前循环路径
        var loopPath = context.GetCurrentPath() + "/for-in";

        // 尝试从缓存中恢复生成器（如果是从 yield 恢复）
        GeneratorLangValue generator;
        var cacheKey = loopPath + "/generator";
        if (context.AsyncStreamCache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is GeneratorLangValue cachedGenerator)
        {
            // 使用缓存的生成器实例
            generator = cachedGenerator;
        }
        else
        {
            // 第一次执行，使用传入的生成器并缓存
            generator = generatorValue;
            context.AsyncStreamCache[cacheKey] = generator;
        }

        // 将循环路径压栈
        context.PathStack.Push("/for-in");

        try
        {
            while (true)
            {
                manager.ControlFlowManager.ResetCurrentState();

                // 关键修复：在每次迭代开始时，如果上一次迭代完成了 yield，清除 ExecutionPath
                // 这样新的迭代可以完整执行，不会被 BlockStatement 的恢复逻辑跳过
                if (!string.IsNullOrEmpty(context.ExecutionPath) && !context.HasYielded)
                {
                    context.ExecutionPath = "";
                }

                // 调用生成器的 Run 方法获取下一个值
                generator.Run(manager);

                if (generator.State == GeneratorLangValue.GeneratorState.Completed)
                {
                    // 生成器完成，从缓存中移除
                    context.AsyncStreamCache.Remove(cacheKey);
                    break;
                }

                if (generator.State == GeneratorLangValue.GeneratorState.Suspended)
                {
                    var currentValue = generator.NextValue;

                    if (currentValue is not null && currentValue is not VoidLangValue)
                    {
                        manager.Set(id, currentValue);
                        body.Run(manager);

                        // 检查外部生成器是否 yield
                        if (context.HasYielded)
                        {
                            // 外部生成器 yield 了，需要保存当前状态并退出
                            // 生成器已经在缓存中，下次恢复时会继续
                            return;
                        }

                        if (manager.ControlFlowManager.BreakFlag)
                        {
                            manager.ControlFlowManager.BreakFlag = false;
                            // Break 时也要清除缓存
                            context.AsyncStreamCache.Remove(cacheKey);
                            break;
                        }

                        if (manager.ControlFlowManager.ContinueFlag)
                        {
                            manager.ControlFlowManager.ContinueFlag = false;
                        }
                    }
                    else
                    {
                        // 如果返回 VoidLangValue，生成器已完成
                        context.AsyncStreamCache.Remove(cacheKey);
                        break;
                    }
                }
                else
                {
                    // 如果既不是Completed也不是Suspended，则退出循环
                    context.AsyncStreamCache.Remove(cacheKey);
                    break;
                }
            }
        }
        finally
        {
            // 弹出循环路径
            context.PathStack.Pop();
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

        // 尝试确定元素类型并进行类型转换
        var exprType = expression.OutputType(local);
        if (exprType is not null)
        {
            // 如果是数组类型
            if (exprType.IsArray && exprType.GetElementType() == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                ilGenerator.Emit(OpCodes.Box, typeof(int)); // 重新装箱以保持统一性
            }
            // 如果是泛型集合且元素类型是 int
            else if (exprType.IsGenericType && exprType.GetGenericArguments().Length > 0
                     && exprType.GetGenericArguments()[0] == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                ilGenerator.Emit(OpCodes.Box, typeof(int)); // 重新装箱以保持统一性
            }
        }

        ilGenerator.Emit(OpCodes.Stloc, current);

        // 处理标识符赋值
        if (AllIds.Count == 1)
        {
            // 单个标识符，直接赋值
            // 在异步状态机中，需要定义变量并存储
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, AllIds[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, AllIds[0].IdName, Position);
            }
            else
            {
                local.AddLocalVar(AllIds[0].IdName, current);
            }
        }
        else
        {
            // 多个标识符，只赋值给第一个
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, AllIds[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, AllIds[0].IdName, Position);
            }
            else
            {
                local.AddLocalVar(AllIds[0].IdName, current);
            }
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

        // 在循环外部声明键和值的局部变量
        var keyLocal = ilGenerator.DeclareLocal(typeof(object));
        LocalBuilder? valueLocal = null;
        if (AllIds.Count > 1)
        {
            valueLocal = ilGenerator.DeclareLocal(typeof(object));
        }

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
        ilGenerator.Emit(OpCodes.Stloc, keyLocal);

        // 将键添加到局部变量管理器
        if (local.AsyncStateMachineGenerator != null)
        {
            local.DefineVariable(ilGenerator, AllIds[0].IdName, typeof(object));
            ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
            local.StoreVariable(ilGenerator, AllIds[0].IdName, Position);
        }
        else
        {
            local.AddLocalVar(AllIds[0].IdName, keyLocal);
        }

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
            ilGenerator.Emit(OpCodes.Stloc, valueLocal!);

            // 将值添加到局部变量管理器
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, AllIds[1].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, valueLocal!);
                local.StoreVariable(ilGenerator, AllIds[1].IdName, Position);
            }
            else
            {
                local.AddLocalVar(AllIds[1].IdName, valueLocal!);
            }
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