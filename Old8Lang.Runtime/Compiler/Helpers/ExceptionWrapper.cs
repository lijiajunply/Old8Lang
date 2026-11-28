namespace Old8Lang.Compiler.Helpers;

/// <summary>
/// 异常包装类,用于在 IL 模式下存储异常对象
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

    /// <summary>
    /// 把捕获到的异常绑定成 catch 变量应持有的值。
    /// </summary>
    /// <remarks>
    /// <see cref="LangThrownException"/> 携带的是语言层 <c>throw</c> 的原始值
    /// （字符串、字典、类实例等），这里解包回原生值；
    /// 其余异常（运行期错误等）仍然包装成 <see cref="ExceptionWrapper"/>。
    /// </remarks>
    public static object? Bind(Exception exception)
    {
        // 反射/委托调用会把语言异常再包一层 TargetInvocationException，
        // 逐层剥掉，否则 catch 变量拿到的是通用 reflection 消息。
        while (exception is System.Reflection.TargetInvocationException { InnerException: not null } invocation)
        {
            exception = invocation.InnerException;
        }

        if (exception is LangThrownException thrown)
        {
            return InstanceMethods.Core.IlValueBridge.Unwrap(thrown.Value);
        }

        return new ExceptionWrapper(exception);
    }
}
