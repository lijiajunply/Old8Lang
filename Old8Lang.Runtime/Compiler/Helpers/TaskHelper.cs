namespace Old8Lang.Compiler.Helpers;

/// <summary>
/// Task辅助类，提供 IL 模式下Task操作的辅助方法
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

    /// <summary>
    /// 把 <c>Task.FromException</c> 的实参转换成 <see cref="Exception"/>。
    /// </summary>
    /// <remarks>
    /// 解释器/虚拟机里的 <c>Task.FromException("错误")</c> 允许传字符串（或任意值），
    /// 而 .NET 的 <c>Task.FromException&lt;T&gt;(Exception)</c> 只接受异常对象，
    /// 这里做一次转换，让 IL 模式与其它模式语义一致。
    /// </remarks>
    public static Exception ToException(object? value) => value switch
    {
        Exception exception => exception,
        null => new Exception("Unknown error"),
        _ => new Exception(value.ToString())
    };
}
