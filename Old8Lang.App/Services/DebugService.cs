namespace Old8Lang.App.Services;

/// <summary>
/// 调试服务
/// </summary>
public static class DebugService
{
    private static Old8Lang.Debugger.Debugger? _debugger;

    /// <summary>
    /// 获取调试器实例
    /// </summary>
    /// <returns>调试器实例</returns>
    public static Old8Lang.Debugger.Debugger? GetDebugger()
    {
        return _debugger;
    }

    /// <summary>
    /// 设置调试器实例
    /// </summary>
    /// <param name="debugger">调试器实例</param>
    public static void SetDebugger(Old8Lang.Debugger.Debugger debugger)
    {
        _debugger = debugger;
    }

    /// <summary>
    /// 清除调试器实例
    /// </summary>
    public static void ClearDebugger()
    {
        _debugger = null;
    }
}