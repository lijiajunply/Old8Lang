namespace Old8Lang.Error;

/// <summary>
/// 编译异常类
/// </summary>
public class CompilerException : Exception
{
    /// <summary>
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="position">源代码位置</param>
    public CompilerException(string message, SourcePosition position) : base(message)
    {
        Position = position;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="position">源代码位置</param>
    /// <param name="innerException">内部异常</param>
    public CompilerException(string message, SourcePosition position, Exception innerException) : base(message, innerException)
    {
        Position = position;
    }
    
    public override string ToString()
    {
        return $"{Message} (位置: {Position})";
    }
}