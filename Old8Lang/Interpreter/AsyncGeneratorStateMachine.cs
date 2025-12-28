using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Generators;

namespace Old8Lang.Interpreter;

/// <summary>
/// 异步生成器状态机（新架构）
/// 使用 GeneratorExecutionContext 进行状态管理，支持状态保存和恢复
/// </summary>
public class AsyncGeneratorStateMachine
{
    private readonly AsyncFuncLangValue AsyncFunc;
    private readonly VariateManager Manager;
    private readonly CancellationToken CancellationToken;
    private readonly GeneratorAstScanner.ScanResult ScanResult;

    private int StatePoint;
    private readonly Dictionary<string, LangValueType> Locals = new();

    /// <summary>
    /// 当前值
    /// </summary>
    public LangValueType? Current { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asyncFunc">异步函数</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="cancellationToken">取消令牌</param>
    public AsyncGeneratorStateMachine(
        AsyncFuncLangValue asyncFunc,
        VariateManager manager,
        CancellationToken cancellationToken)
    {
        AsyncFunc = asyncFunc;
        Manager = manager;
        CancellationToken = cancellationToken;

        // 扫描 AST 获取生成器信息
        var scanner = new GeneratorAstScanner();
        ScanResult = scanner.Scan(asyncFunc.BlockStatement);
    }

    /// <summary>
    /// 异步移动到下一个值
    /// </summary>
    /// <returns>如果还有下一个值返回 true，否则返回 false</returns>
    public async Task<bool> MoveNextAsync()
    {
        CancellationToken.ThrowIfCancellationRequested();

        // 在异步任务中执行生成器逻辑
        var result = await Task.Run(() => ExecuteStep(), CancellationToken);

        if (result.HasValue)
        {
            Current = result.YieldValue;
            StatePoint = result.NextState;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 执行一步生成器逻辑
    /// </summary>
    private StateExecutor.ExecutionResult ExecuteStep()
    {
        // 注意：不要在这里调用 AddChildren()，因为异步生成器需要跨 yield 保持所有变量状态
        // 变量应该保存在外部作用域或通过 _locals 字典管理

        // 恢复局部变量
        RestoreLocals();

        // 创建生成器上下文
        var context = new GeneratorExecutionContext();
        Manager.GeneratorContext = context;

        // 如果不是首次执行，设置恢复信息
        if (StatePoint > 0)
        {
            // 从 locals 中恢复上一次 yield 的路径
            if (Locals.TryGetValue("__last_yield_path__", out var pathValue) && pathValue is StringLangValue strValue)
            {
                context.ExecutionPath = strValue.Value;
            }

            // 从 locals 中恢复循环状态到 LoopStates
            RestoreLoopStates(context);
        }

        try
        {
            // 执行函数体直到下一个 yield
            AsyncFunc.BlockStatement.Run(Manager);

            // 检查是否 yield
            if (context.HasYielded)
            {
                // 保存当前 yield 的路径
                if (!string.IsNullOrEmpty(context.ExecutionPath))
                {
                    Locals["__last_yield_path__"] = new StringLangValue(context.ExecutionPath);
                }

                // 保存局部变量
                SaveLocals();

                // 保存循环状态
                SaveLoopStates(context);

                // 返回 yield 的值
                return StateExecutor.ExecutionResult.Yield(context.CurrentValue!, StatePoint + 1);
            }
            else
            {
                // 函数执行完毕
                return StateExecutor.ExecutionResult.Complete();
            }
        }
        finally
        {
            Manager.GeneratorContext = null;
        }
    }

    /// <summary>
    /// 恢复局部变量到环境
    /// </summary>
    private void RestoreLocals()
    {
        foreach (var kvp in Locals)
        {
            // 跳过循环状态变量（以 __loop__ 开头）
            if (kvp.Key.StartsWith("__loop__"))
                continue;

            // 跳过异步流缓存变量（以 __stream__ 开头）
            if (kvp.Key.StartsWith("__stream__"))
                continue;

            // 跳过执行路径变量
            if (kvp.Key == "__last_yield_path__")
                continue;

            var id = new LangId(kvp.Key);
            Manager.Set(id, kvp.Value);
        }
    }

    /// <summary>
    /// 保存环境中的局部变量
    /// </summary>
    private void SaveLocals()
    {
        // 清除旧的局部变量（但保留循环状态、异步流缓存和执行路径）
        var loopStates = Locals.Where(kvp => kvp.Key.StartsWith("__loop__")).ToList();
        var streamCache = Locals.Where(kvp => kvp.Key.StartsWith("__stream__")).ToList();
        var yieldPath = Locals.ContainsKey("__last_yield_path__") ? Locals["__last_yield_path__"] : null;

        Locals.Clear();

        foreach (var loopState in loopStates)
        {
            Locals[loopState.Key] = loopState.Value;
        }

        foreach (var stream in streamCache)
        {
            Locals[stream.Key] = stream.Value;
        }

        if (yieldPath != null)
        {
            Locals["__last_yield_path__"] = yieldPath;
        }

        // 保存所有扫描到的局部变量
        foreach (var varName in ScanResult.LocalVariables)
        {
            var value = Manager.GetAny(new LangId(varName));
            if (value != null)
            {
                Locals[varName] = value;
            }
        }
    }

    /// <summary>
    /// 从 locals 恢复循环状态到 context.LoopStates 和 AsyncStreamCache
    /// </summary>
    private void RestoreLoopStates(GeneratorExecutionContext context)
    {
        foreach (var kvp in Locals)
        {
            if (kvp.Key.StartsWith("__loop__") && kvp.Value is IntLangValue intValue)
            {
                // 提取循环路径：__loop__/block[0]/for-in -> /block[0]/for-in
                var loopPath = kvp.Key.Substring("__loop__".Length);
                context.LoopStates[loopPath] = intValue.Value;
            }
            else if (kvp.Key.StartsWith("__stream__"))
            {
                // 恢复异步流缓存：__stream__/block[0]/async-for-in -> /block[0]/async-for-in
                var streamPath = kvp.Key.Substring("__stream__".Length);
                context.AsyncStreamCache[streamPath] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// 保存循环状态从 context.LoopStates 和 AsyncStreamCache 到 locals
    /// </summary>
    private void SaveLoopStates(GeneratorExecutionContext context)
    {
        // 从 LoopStates 字典保存所有循环状态
        foreach (var kvp in context.LoopStates)
        {
            var key = "__loop__" + kvp.Key;
            Locals[key] = new IntLangValue(kvp.Value);
        }

        // 从 AsyncStreamCache 保存所有异步流实例
        foreach (var kvp in context.AsyncStreamCache)
        {
            var key = "__stream__" + kvp.Key;
            if (kvp.Value is LangValueType langValue)
            {
                Locals[key] = langValue;
            }
        }
    }

    /// <summary>
    /// 重置状态机
    /// </summary>
    public void Reset()
    {
        StatePoint = 0;
        Locals.Clear();
        Current = null;
    }
}
