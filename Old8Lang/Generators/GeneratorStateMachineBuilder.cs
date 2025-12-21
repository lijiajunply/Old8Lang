using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Generators;

/// <summary>
/// 生成器状态机构建器
/// 将 AST 转换为可执行的状态机
/// </summary>
public static class GeneratorStateMachineBuilder
{
    /// <summary>
    /// 从函数构建状态机（使用 FuncLangValue）
    /// </summary>
    public static NewGeneratorStateMachine BuildFromFunc(FuncLangValue function, VariateManager manager)
    {
        // 1. 扫描 AST
        var scanner = new GeneratorAstScanner();
        var scanResult = scanner.Scan(function.BlockStatement);

        if (!scanResult.IsGenerator)
        {
            throw new InvalidOperationException("Function does not contain yield statements");
        }

        // 2. 创建执行器
        var executor = new FlatGeneratorExecutor(function, scanResult);

        // 3. 创建并返回状态机（传入 null 作为 FuncInit）
        return new NewGeneratorStateMachine(null!, manager, executor);
    }

    /// <summary>
    /// 从函数定义构建状态机（使用 FuncInit）
    /// </summary>
    public static NewGeneratorStateMachine Build(FuncInit function, VariateManager manager,
        List<LangValueType>? arguments = null)
    {
        // 1. 扫描 AST
        var scanner = new GeneratorAstScanner();
        var scanResult = scanner.Scan(function.FuncLangValue.BlockStatement);

        if (!scanResult.IsGenerator)
        {
            throw new InvalidOperationException("Function does not contain yield statements");
        }

        // 2. 创建执行器
        var executor = new FlatGeneratorExecutor(function.FuncLangValue, scanResult);

        // 3. 创建并返回状态机
        return new NewGeneratorStateMachine(function, manager, executor);
    }
}

/// <summary>
/// 扁平化的生成器执行器（新架构）
/// 采用基于路径的状态恢复机制，避免依赖全局索引
/// </summary>
public class FlatGeneratorExecutor(FuncLangValue function, GeneratorAstScanner.ScanResult scanResult)
    : StateExecutor
{
    public override ExecutionResult Execute(int statePoint, Dictionary<string, LangValueType> locals,
        VariateManager manager)
    {
        // 创建子作用域
        manager.AddChildren();
        try
        {
            // 恢复局部变量
            RestoreLocals(manager, locals);

            // 创建生成器上下文
            var context = new GeneratorExecutionContext();
            manager.GeneratorContext = context;

            // 如果不是首次执行，设置恢复信息
            if (statePoint > 0)
            {
                // 从 locals 中恢复上一次 yield 的路径
                if (locals.TryGetValue("__last_yield_path__", out var pathValue) && pathValue is StringLangValue strValue)
                {
                    // 设置执行路径，让语句知道从哪里恢复
                    context.ExecutionPath = strValue.Value;
                }

                // 从 locals 中恢复循环状态到 LoopStates
                RestoreLoopStates(context, locals);
            }

            try
            {
                // 执行函数体直到下一个 yield
                function.BlockStatement.Run(manager);

                // 检查是否 yield
                if (context.HasYielded)
                {
                    // 保存当前 yield 的路径
                    if (!string.IsNullOrEmpty(context.ExecutionPath))
                    {
                        locals["__last_yield_path__"] = new StringLangValue(context.ExecutionPath);
                    }

                    // 保存局部变量
                    SaveLocals(manager, locals);

                    // 保存循环状态
                    SaveLoopStates(context, locals);

                    // 返回 yield 的值
                    return ExecutionResult.Yield(context.CurrentValue!, statePoint + 1);
                }
                else
                {
                    // 函数执行完毕
                    return ExecutionResult.Complete();
                }
            }
            finally
            {
                manager.GeneratorContext = null;
            }
        }
        finally
        {
            manager.RemoveChildren();
        }
    }

    /// <summary>
    /// 恢复局部变量到环境
    /// </summary>
    private void RestoreLocals(VariateManager manager, Dictionary<string, LangValueType> locals)
    {
        foreach (var kvp in locals)
        {
            // 跳过循环状态变量（以 __loop__ 开头）
            if (kvp.Key.StartsWith("__loop__"))
                continue;

            var id = new LangId(kvp.Key);
            manager.Set(id, kvp.Value);
        }
    }

    /// <summary>
    /// 保存环境中的局部变量
    /// </summary>
    private void SaveLocals(VariateManager manager, Dictionary<string, LangValueType> locals)
    {
        // 清除旧的局部变量（但保留循环状态和执行路径）
        var loopStates = locals.Where(kvp => kvp.Key.StartsWith("__loop__")).ToList();
        var yieldPath = locals.ContainsKey("__last_yield_path__") ? locals["__last_yield_path__"] : null;

        locals.Clear();

        foreach (var loopState in loopStates)
        {
            locals[loopState.Key] = loopState.Value;
        }

        if (yieldPath != null)
        {
            locals["__last_yield_path__"] = yieldPath;
        }

        // 保存所有扫描到的局部变量
        foreach (var varName in scanResult.LocalVariables)
        {
            var value = manager.GetAny(new LangId(varName));
            if (value != null)
            {
                locals[varName] = value;
            }
        }
    }

    /// <summary>
    /// 从 locals 恢复循环状态到 context.LoopStates
    /// </summary>
    private void RestoreLoopStates(GeneratorExecutionContext context, Dictionary<string, LangValueType> locals)
    {
        foreach (var kvp in locals)
        {
            if (kvp.Key.StartsWith("__loop__") && kvp.Value is IntLangValue intValue)
            {
                // 提取循环路径：__loop__/block[0]/for-in -> /block[0]/for-in
                var loopPath = kvp.Key.Substring("__loop__".Length);
                context.LoopStates[loopPath] = intValue.Value;
            }
        }
    }

    /// <summary>
    /// 保存循环状态从 context.LoopStates 到 locals
    /// </summary>
    private void SaveLoopStates(GeneratorExecutionContext context, Dictionary<string, LangValueType> locals)
    {
        // 从 LoopStates 字典保存所有循环状态
        foreach (var kvp in context.LoopStates)
        {
            var key = "__loop__" + kvp.Key;
            locals[key] = new IntLangValue(kvp.Value);
        }
    }
}
