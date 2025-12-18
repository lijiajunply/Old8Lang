using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// 执行上下文，用于在扩展方法中访问当前的 VariateManager
/// </summary>
public static class ExecutionContext
{
    private static readonly AsyncLocal<VariateManager?> CurrentManager = new();

    /// <summary>
    /// 设置当前的 VariateManager
    /// </summary>
    /// <param name="manager">要设置的 VariateManager</param>
    public static void SetCurrentManager(VariateManager manager)
    {
        CurrentManager.Value = manager;
    }

    /// <summary>
    /// 获取当前的 VariateManager
    /// </summary>
    /// <returns>当前的 VariateManager，如果没有设置则返回 null</returns>
    public static VariateManager? GetCurrentManager()
    {
        return CurrentManager.Value;
    }

    /// <summary>
    /// 清除当前的 VariateManager
    /// </summary>
    public static void ClearCurrentManager()
    {
        CurrentManager.Value = null;
    }
}