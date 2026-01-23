namespace Old8Lang.Compiler.Helpers;

/// <summary>
/// 异常包装类,用于在编译器模式下存储异常对象
/// 重写ToString()方法,只返回异常消息而不包含完整的堆栈跟踪
/// </summary>
public class ExceptionWrapper
{
    /// <summary>
    /// 被包装的异常对象
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 创建异常包装对象
    /// </summary>
    /// <param name="exception">要包装的异常</param>
    public ExceptionWrapper(Exception exception)
    {
        Exception = exception;
    }

    /// <summary>
    /// 重写ToString(),只返回异常消息
    /// </summary>
    /// <returns>异常消息</returns>
    public override string ToString()
    {
        return Exception.Message;
    }
}
