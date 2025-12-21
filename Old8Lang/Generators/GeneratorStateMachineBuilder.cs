using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Generators;

/// <summary>
/// 生成器状态机构建器
/// 将 AST 转换为可执行的状态机
/// </summary>
public class GeneratorStateMachineBuilder
{
    /// <summary>
    /// 从函数构建状态机（使用 FuncLangValue）
    /// </summary>
    public static NewGeneratorStateMachine BuildFromFunc(FuncLangValue function, VariateManager manager)
    {
        System.Console.WriteLine($"[BUILD] Building state machine from FuncLangValue");
        System.Console.WriteLine($"[BUILD] Function has BlockStatement: {function.BlockStatement != null}");
        if (function.BlockStatement != null)
        {
            System.Console.WriteLine($"[BUILD] BlockStatement type: {function.BlockStatement.GetType().Name}");
            System.Console.WriteLine($"[BUILD] BlockStatement count: {function.BlockStatement.Count}");
        }

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
    public static NewGeneratorStateMachine Build(FuncInit function, VariateManager manager, List<LangValueType>? arguments = null)
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
/// 扁平化的生成器执行器（优化版）
/// 采用基于路径的状态恢复机制，避免依赖全局 CurrentStatementIndex
/// </summary>
public class FlatGeneratorExecutor : StateExecutor
{
    private readonly FuncLangValue _function;
    private readonly GeneratorAstScanner.ScanResult _scanResult;

    public FlatGeneratorExecutor(FuncLangValue function, GeneratorAstScanner.ScanResult scanResult)
    {
        _function = function;
        _scanResult = scanResult;
    }

    public override ExecutionResult Execute(int statePoint, Dictionary<string, LangValueType> locals, VariateManager manager)
    {
        System.Console.WriteLine($"[NEW SM] Execute called: statePoint={statePoint}, totalYieldPoints={_scanResult.YieldPoints.Count}");

        // 创建子作用域
        manager.AddChildren();
        try
        {
            // 恢复局部变量
            RestoreLocals(manager, locals);

            // 创建生成器上下文（用于兼容现有的 yield 语句）
            var context = new GeneratorExecutionContext();
            manager.GeneratorContext = context;

            // 如果不是首次执行，设置恢复信息
            if (statePoint > 0)
            {
                // statePoint 是执行次数，不是 yield 点索引
                // 对于循环中的 yield，可能多次执行同一个 yield 点
                // 我们使用 (statePoint - 1) % YieldPoints.Count 来获取上一个 yield 点
                var lastYieldIndex = (statePoint - 1) % _scanResult.YieldPoints.Count;
                var lastYieldPoint = _scanResult.YieldPoints[lastYieldIndex];

                System.Console.WriteLine($"[NEW SM] Resuming from yield point {lastYieldIndex}, path={lastYieldPoint.Path}");

                // 设置执行路径和循环状态（新架构）
                context.ExecutionPath = lastYieldPoint.Path;

                // 从 locals 中恢复循环状态
                // 这会设置 ExecutionStack，让 BlockStatement 和 ForInStatement 知道从哪里恢复
                RestoreLoopStates(context, locals);

                // 兼容旧架构：设置 CurrentStatementIndex
                // yield 语句执行后，循环体的 BlockStatement 应该继续执行下一条语句
                // 从路径可以看出 yield 在哪个位置，例如 "/block[0]/for-in/block[1]/yield" 表示在 block[1]
                // 提取最后一个 block[N] 的索引
                var pathSegments = lastYieldPoint.Path.Split('/');
                int yieldStatementIndex = 1; // 默认假设是第 1 个语句（索引 1）

                for (int i = pathSegments.Length - 1; i >= 0; i--)
                {
                    if (pathSegments[i].StartsWith("block[") && pathSegments[i].EndsWith("]"))
                    {
                        var indexStr = pathSegments[i].Substring(6, pathSegments[i].Length - 7);
                        if (int.TryParse(indexStr, out var index))
                        {
                            yieldStatementIndex = index;
                            break;
                        }
                    }
                }

                // CurrentStatementIndex 设置为 yield 语句之后的索引
                // 这会被循环体的 BlockStatement 使用
                context.CurrentStatementIndex = yieldStatementIndex + 1;
                System.Console.WriteLine($"[NEW SM] Set CurrentStatementIndex={context.CurrentStatementIndex} (yield was at index {yieldStatementIndex})");
            }

            try
            {
                // 执行函数体直到下一个 yield
                _function.BlockStatement.Run(manager);

                // 检查是否 yield
                if (context.HasYielded)
                {
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

            var id = new LangId(kvp.Key, "", null, new SourcePosition());
            manager.Set(id, kvp.Value);
        }
    }

    /// <summary>
    /// 保存环境中的局部变量
    /// </summary>
    private void SaveLocals(VariateManager manager, Dictionary<string, LangValueType> locals)
    {
        // 清除旧的局部变量（但保留循环状态）
        var loopStates = locals.Where(kvp => kvp.Key.StartsWith("__loop__")).ToList();
        locals.Clear();
        foreach (var loopState in loopStates)
        {
            locals[loopState.Key] = loopState.Value;
        }

        // 保存所有扫描到的局部变量
        foreach (var varName in _scanResult.LocalVariables)
        {
            var value = manager.GetAny(new LangId(varName, "", null, new SourcePosition()));
            if (value != null)
            {
                locals[varName] = value;
            }
        }
    }

    /// <summary>
    /// 从 locals 恢复循环状态到 context
    /// 兼容旧架构：恢复 ExecutionStack 用于 ForInStatement 和 BlockStatement
    /// </summary>
    private void RestoreLoopStates(GeneratorExecutionContext context, Dictionary<string, LangValueType> locals)
    {
        // 恢复到 LoopStates（新架构）
        foreach (var kvp in locals)
        {
            if (kvp.Key.StartsWith("__loop__") && kvp.Value is IntLangValue intValue)
            {
                var loopPath = kvp.Key.Substring("__loop__".Length);
                context.LoopStates[loopPath] = intValue.Value;

                // 兼容旧架构：同时恢复到 ExecutionStack
                // 注意压入顺序：先压入 "for_in_loop_position"（会被 ForInStatement 弹出）
                // 再压入 "loop"（会被 BlockStatement 看到）
                context.ExecutionStack.Push(new GeneratorExecutionContext.BlockExecutionFrame
                {
                    StatementIndex = -1,
                    BlockId = "for_in_loop_position",
                    LoopIteration = intValue.Value
                });

                context.ExecutionStack.Push(new GeneratorExecutionContext.BlockExecutionFrame
                {
                    StatementIndex = 0,  // ForInStatement 在外层 BlockStatement 的索引 0
                    BlockId = "loop"
                });

                System.Console.WriteLine($"[NEW SM] Restored loop state: path={loopPath}, iteration={intValue.Value}");
            }
        }
    }

    /// <summary>
    /// 保存循环状态从 context 到 locals
    /// 兼容旧架构：从 ExecutionStack 提取循环状态
    /// </summary>
    private void SaveLoopStates(GeneratorExecutionContext context, Dictionary<string, LangValueType> locals)
    {
        // 新架构：从 LoopStates 字典保存
        foreach (var kvp in context.LoopStates)
        {
            locals["__loop__" + kvp.Key] = new IntLangValue(kvp.Value);
            System.Console.WriteLine($"[NEW SM] Saved loop state from LoopStates: path={kvp.Key}, iteration={kvp.Value}");
        }

        // 兼容旧架构：从 ExecutionStack 提取循环位置并保存
        // ForInStatement 会压入 BlockExecutionFrame 到 ExecutionStack
        var loopFrames = context.ExecutionStack.Where(f => f.BlockId == "for_in_loop_position" && f.LoopIteration.HasValue).ToList();

        int loopIndex = 0;
        foreach (var frame in loopFrames)
        {
            var key = $"__loop__/for-in-{loopIndex}";
            locals[key] = new IntLangValue(frame.LoopIteration!.Value);
            System.Console.WriteLine($"[NEW SM] Saved loop state from ExecutionStack: key={key}, iteration={frame.LoopIteration.Value}");
            loopIndex++;
        }
    }
}
