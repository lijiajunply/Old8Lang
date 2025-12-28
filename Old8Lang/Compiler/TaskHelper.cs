namespace Old8Lang.Compiler;

/// <summary>
/// Task辅助类，提供编译模式下Task操作的辅助方法
/// </summary>
public static class TaskHelper
{
    /// <summary>
    /// 返回null的辅助方法，用于Task.Delay的延续
    /// </summary>
    public static object? ReturnNull(Task task)
    {
        return null;
    }
}
