using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.Interpreter;

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
/// 扁平化的生成器执行器
/// 采用状态机方式执行，避免递归调用
/// </summary>
public class FlatGeneratorExecutor : StateExecutor
{
    private readonly FuncLangValue _function;
    private readonly GeneratorAstScanner.ScanResult _scanResult;

    // 状态机的局部变量存储
    private Dictionary<string, LangValueType>? _savedLocals;

    public FlatGeneratorExecutor(FuncLangValue function, GeneratorAstScanner.ScanResult scanResult)
    {
        _function = function;
        _scanResult = scanResult;
    }

    public override ExecutionResult Execute(int statePoint, Dictionary<string, LangValueType> locals, VariateManager manager)
    {
        // 如果已经执行完所有 yield 点
        if (statePoint >= _scanResult.YieldPoints.Count)
        {
            return ExecutionResult.Complete();
        }

        // 创建子作用域
        manager.AddChildren();
        try
        {
            // 恢复局部变量
            RestoreLocals(manager, locals);

            // 创建生成器上下文（用于兼容现有的 yield 语句）
            var context = new GeneratorExecutionContext();
            manager.GeneratorContext = context;

            try
            {
                // 执行函数体直到下一个 yield
                if (statePoint == 0)
                {
                    // 第一次执行：从头开始
                    _function.BlockStatement.Run(manager);
                }
                else
                {
                    // 继续执行：设置状态点并继续
                    context.CurrentStatementIndex = statePoint;
                    _function.BlockStatement.Run(manager);
                }

                // 检查是否 yield
                if (context.HasYielded)
                {
                    // 保存局部变量
                    SaveLocals(manager, locals);

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
            var id = new LangId(kvp.Key, "", null, new SourcePosition());
            manager.Set(id, kvp.Value);
        }
    }

    /// <summary>
    /// 保存环境中的局部变量
    /// </summary>
    private void SaveLocals(VariateManager manager, Dictionary<string, LangValueType> locals)
    {
        locals.Clear();

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
}
