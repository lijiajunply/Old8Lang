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
public partial class AsyncForInStatement(
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
                                    var values = new List<LangValueType>
                                        { tupleValue.Value.Item1, tupleValue.Value.Item2 };

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
                                    var values = new List<LangValueType>
                                        { tupleValue.Value.Item1, tupleValue.Value.Item2 };

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
                                    var values = new List<LangValueType>
                                        { tupleValue.Value.Item1, tupleValue.Value.Item2 };

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
                throw new TypeError(this,
                    "ILangList、GeneratorLangValue、AsyncGeneratorLangValue、AsyncStreamLangValue 或 TaskLangValue",
                    value.GetType().Name);
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
            // 获取当前循环路径，用于缓存异步流实例
            var loopPath = context.GetCurrentPath() + "/async-for-in";

            // 尝试从缓存中获取异步流实例
            LangValueType? value = null;
            if (context.AsyncStreamCache.TryGetValue(loopPath, out var cachedValue))
            {
                value = cachedValue as LangValueType;
            }

            // 如果缓存中没有，则evaluate表达式并缓存
            if (value == null)
            {
                value = expression.Run(manager);

                // 处理 TaskLangValue - await 任务并获取结果
                if (value is TaskLangValue taskValue)
                {
                    value = taskValue.Await();
                }

                // 缓存异步流或异步生成器实例
                if (value is AsyncStreamLangValue || value is AsyncGeneratorLangValue)
                {
                    context.AsyncStreamCache[loopPath] = value;
                }
            }

            // 处理 AsyncStreamLangValue
            if (value is AsyncStreamLangValue asyncStream)
            {
                RunGeneratorContextAsyncStream(manager, asyncStream, context, loopPath);
                return;
            }

            // 处理 AsyncGeneratorLangValue
            if (value is AsyncGeneratorLangValue asyncGenerator)
            {
                RunGeneratorContextAsyncGenerator(manager, asyncGenerator, context, loopPath);
                return;
            }

            // 处理普通列表对象：需要支持从上次 yield 位置恢复
            if (value is ILangList oldList)
            {
                RunGeneratorContextList(manager, oldList, context);
                return;
            }

            throw new TypeError(this,
                "ILangList、GeneratorLangValue、AsyncGeneratorLangValue、AsyncStreamLangValue 或 TaskLangValue",
                value.GetType().Name);
        }
        finally
        {
            manager.ControlFlowManager.PopState();
        }
    }

    /// <summary>
    /// 在生成器上下文中迭代异步流
    /// 异步流自己管理状态，可以跨yield保持状态
    ///
    /// 关键设计：当循环体yield时，我们保存当前的执行路径，
    /// 但将ExecutionPath修改为指向async-for语句本身，
    /// 这样下次恢复时会重新进入这个方法继续循环
    /// </summary>
    private void RunGeneratorContextAsyncStream(VariateManager manager, AsyncStreamLangValue asyncStream,
        Old8Lang.Generators.GeneratorExecutionContext context, string loopPath)
    {
        // 使用 LoopStates 来追踪循环状态
        // 0: 循环正在进行中
        // 1: 循环从yield恢复，需要继续下一次迭代
        var loopStateKey = loopPath + "/state";

        // 检查是否从yield恢复
        bool resumingFromYield = context.LoopStates.TryGetValue(loopStateKey, out var state) && state == 1;

        if (context.LoopStates.TryAdd(loopStateKey, 0))
        {
            // 首次进入循环，清除ExecutionPath避免干扰
            context.ExecutionPath = "";
        }
        else if (resumingFromYield)
        {
            // 从yield恢复，重置状态为正常迭代
            context.LoopStates[loopStateKey] = 0;
            // 清除ExecutionPath，让循环体能够正常执行
            context.ExecutionPath = "";
        }

        // 将循环路径压栈
        context.PathStack.Push("/async-for-in");

        try
        {
            // 标准异步流迭代，在生成器上下文中
            while (true)
            {
                manager.ControlFlowManager.ResetCurrentState();

                var nextValue = asyncStream.Run(manager);

                if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
                {
                    // 异步流完成，清除缓存和状态
                    context.AsyncStreamCache.Remove(loopPath);
                    context.LoopStates.Remove(loopStateKey);
                    break;
                }

                if (asyncStream.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
                {
                    var currentValue = asyncStream.NextValue;

                    if (currentValue != null && !(currentValue is VoidLangValue))
                    {
                        // 设置循环变量
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

                        // 如果循环体中发生了yield，我们需要：
                        // 1. 标记循环状态为"从yield恢复"
                        // 2. 修改ExecutionPath指向async-for语句，而不是循环体内部
                        // 这样下次恢复时会重新进入这个方法继续循环
                        if (context.HasYielded)
                        {
                            // 标记为从yield恢复状态
                            context.LoopStates[loopStateKey] = 1;
                            // 将ExecutionPath设置为async-for-in语句的路径
                            // 这样下次恢复时会重新调用RunWithGeneratorContext -> RunGeneratorContextAsyncStream
                            context.ExecutionPath = loopPath;
                            return;
                        }

                        if (manager.ControlFlowManager.BreakFlag)
                        {
                            manager.ControlFlowManager.BreakFlag = false;
                            // 清除缓存和状态
                            context.AsyncStreamCache.Remove(loopPath);
                            context.LoopStates.Remove(loopStateKey);
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

            // 循环正常完成，清除ExecutionPath
            context.ExecutionPath = "";
        }
        finally
        {
            // 弹出循环路径
            context.PathStack.Pop();
        }
    }

    /// <summary>
    /// 在生成器上下文中迭代异步生成器
    /// 与异步流类似，需要在 yield 后能够恢复执行
    /// </summary>
    private void RunGeneratorContextAsyncGenerator(VariateManager manager, AsyncGeneratorLangValue asyncGenerator,
        Old8Lang.Generators.GeneratorExecutionContext context, string loopPath)
    {
        // 使用 LoopStates 来追踪循环状态
        // 0: 循环正在进行中
        // 1: 循环从yield恢复，需要继续下一次迭代
        var loopStateKey = loopPath + "/state";

        // 检查是否从yield恢复
        bool resumingFromYield = context.LoopStates.TryGetValue(loopStateKey, out var state) && state == 1;

        if (context.LoopStates.TryAdd(loopStateKey, 0))
        {
            // 首次进入循环，清除ExecutionPath避免干扰
            context.ExecutionPath = "";
        }
        else if (resumingFromYield)
        {
            // 从yield恢复，重置状态为正常迭代
            context.LoopStates[loopStateKey] = 0;
            // 清除ExecutionPath，让循环体能够正常执行
            context.ExecutionPath = "";
        }

        // 将循环路径压栈
        context.PathStack.Push("/async-for-in");

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
                    // 异步生成器完成，清除缓存和状态
                    context.AsyncStreamCache.Remove(loopPath);
                    context.LoopStates.Remove(loopStateKey);
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
                            // 设置循环变量
                            if (AllIds.Count == 1)
                            {
                                manager.Set(id, currentValue);
                            }
                            else
                            {
                                if (currentValue is TupleLangValue tupleValue)
                                {
                                    tupleValue.Run(manager);
                                    var values = new List<LangValueType>
                                        { tupleValue.Value.Item1, tupleValue.Value.Item2 };
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

                            // 如果循环体中发生了yield，我们需要：
                            // 1. 标记循环状态为"从yield恢复"
                            // 2. 修改ExecutionPath指向async-for语句，而不是循环体内部
                            // 这样下次恢复时会重新进入这个方法继续循环
                            if (context.HasYielded)
                            {
                                // 标记为从yield恢复状态
                                context.LoopStates[loopStateKey] = 1;
                                // 将ExecutionPath设置为async-for-in语句的路径
                                // 这样下次恢复时会重新调用RunWithGeneratorContext -> RunGeneratorContextAsyncGenerator
                                context.ExecutionPath = loopPath;
                                return;
                            }

                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                manager.ControlFlowManager.BreakFlag = false;
                                // 清除缓存和状态
                                context.AsyncStreamCache.Remove(loopPath);
                                context.LoopStates.Remove(loopStateKey);
                                break;
                            }

                            if (manager.ControlFlowManager.ContinueFlag)
                            {
                                manager.ControlFlowManager.ContinueFlag = false;
                                continue;
                            }
                        }
                        finally
                        {
                            // 清除 GeneratorContext 以继续迭代内层生成器
                            manager.GeneratorContext = null;
                        }
                    }
                }
            }

            // 循环正常完成，清除ExecutionPath
            context.ExecutionPath = "";
        }
        finally
        {
            // 恢复外层的 GeneratorContext
            manager.GeneratorContext = outerContext;
            // 弹出循环路径
            context.PathStack.Pop();
        }
    }

    /// <summary>
    /// 在生成器上下文中迭代普通列表（新架构）
    /// </summary>
    private void RunGeneratorContextList(VariateManager manager, ILangList oldList,
        Old8Lang.Generators.GeneratorExecutionContext context)
    {
        var items = oldList.GetItems().ToList();

        // 获取当前循环路径
        var loopPath = context.GetCurrentPath() + "/async-for-in";

        // 从循环状态中恢复起始索引
        int startIndex = 0;
        if (context.LoopStates.TryGetValue(loopPath, out var savedIndex))
        {
            startIndex = savedIndex;
        }

        // 将循环路径压栈
        context.PathStack.Push("/async-for-in");

        try
        {
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

                // 执行循环体
                body.Run(manager);

                // 检查是否遇到 yield
                if (context.HasYielded)
                {
                    // 保存当前循环位置（下次从下一个元素开始）
                    context.LoopStates[loopPath] = i + 1;
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
                    continue;
                }
            }

            // 循环完成，清除循环状态
            context.LoopStates.Remove(loopPath);
        }
        finally
        {
            // 弹出循环路径
            context.PathStack.Pop();
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