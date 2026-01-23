using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 并发错误基类
/// </summary>
public class ConcurrencyError : RuntimeError
{
    /// <summary>
    /// 并发错误代码
    /// </summary>
    public new const string ErrorCode = "CONCURRENCY_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ConcurrencyError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查并发操作是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected ConcurrencyError(IOldLangTree node, string errorCode, string message, string suggestion)
        : base(
            node,
            errorCode,
            message,
            suggestion)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public ConcurrencyError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查并发操作是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected ConcurrencyError(SourcePosition position, string errorCode, string message, string suggestion)
        : base(
            position,
            errorCode,
            message,
            suggestion)
    {
    }
}

/// <summary>
/// 死锁错误
/// </summary>
public class DeadlockError : ConcurrencyError
{
    /// <summary>
    /// 死锁错误代码
    /// </summary>
    public new const string ErrorCode = "DEADLOCK_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    public DeadlockError(IOldLangTree node)
        : base(
            node,
            ErrorCode,
            "检测到死锁: 多个线程相互等待对方持有的资源",
            "请检查锁的获取顺序，确保所有线程以相同的顺序获取锁")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public DeadlockError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            $"检测到死锁: {message}",
            "请检查锁的获取顺序，确保所有线程以相同的顺序获取锁")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public DeadlockError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            $"检测到死锁: {message}",
            "请检查锁的获取顺序，确保所有线程以相同的顺序获取锁")
    {
    }
}

/// <summary>
/// Channel 已关闭错误
/// </summary>
public class ChannelClosedError : ConcurrencyError
{
    /// <summary>
    /// Channel 已关闭错误代码
    /// </summary>
    public new const string ErrorCode = "CHANNEL_CLOSED_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="channelId">Channel ID</param>
    public ChannelClosedError(IOldLangTree node, int channelId)
        : base(
            node,
            ErrorCode,
            $"Channel {channelId} 已关闭，无法进行发送或接收操作",
            "请在操作前检查 Channel 是否已关闭")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ChannelClosedError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请在操作前检查 Channel 是否已关闭")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="channelId">Channel ID</param>
    public ChannelClosedError(SourcePosition position, int channelId)
        : base(
            position,
            ErrorCode,
            $"Channel {channelId} 已关闭，无法进行发送或接收操作",
            "请在操作前检查 Channel 是否已关闭")
    {
    }
}

/// <summary>
/// Channel 已满错误
/// </summary>
public class ChannelFullError : ConcurrencyError
{
    /// <summary>
    /// Channel 已满错误代码
    /// </summary>
    public new const string ErrorCode = "CHANNEL_FULL_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="capacity">Channel 容量</param>
    public ChannelFullError(IOldLangTree node, int channelId, int capacity)
        : base(
            node,
            ErrorCode,
            $"Channel {channelId} 已满 (容量: {capacity})，无法发送更多数据",
            "请等待消费者接收数据，或使用带超时的 TrySend 方法")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ChannelFullError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请等待消费者接收数据，或使用带超时的 TrySend 方法")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="capacity">Channel 容量</param>
    public ChannelFullError(SourcePosition position, int channelId, int capacity)
        : base(
            position,
            ErrorCode,
            $"Channel {channelId} 已满 (容量: {capacity})，无法发送更多数据",
            "请等待消费者接收数据，或使用带超时的 TrySend 方法")
    {
    }
}

/// <summary>
/// 锁获取失败错误
/// </summary>
public class LockAcquisitionError : ConcurrencyError
{
    /// <summary>
    /// 锁获取失败错误代码
    /// </summary>
    public new const string ErrorCode = "LOCK_ACQUISITION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="lockId">锁 ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public LockAcquisitionError(IOldLangTree node, int lockId, int timeoutMs)
        : base(
            node,
            ErrorCode,
            $"无法在 {timeoutMs} 毫秒内获取锁 {lockId}",
            "请检查是否存在死锁，或增加超时时间")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public LockAcquisitionError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查是否存在死锁，或增加超时时间")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="lockId">锁 ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public LockAcquisitionError(SourcePosition position, int lockId, int timeoutMs)
        : base(
            position,
            ErrorCode,
            $"无法在 {timeoutMs} 毫秒内获取锁 {lockId}",
            "请检查是否存在死锁，或增加超时时间")
    {
    }
}

/// <summary>
/// 资源已释放错误
/// </summary>
public class ResourceDisposedError : ConcurrencyError
{
    /// <summary>
    /// 资源已释放错误代码
    /// </summary>
    public new const string ErrorCode = "RESOURCE_DISPOSED_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源 ID</param>
    public ResourceDisposedError(IOldLangTree node, string resourceType, int resourceId)
        : base(
            node,
            ErrorCode,
            $"{resourceType} {resourceId} 已被释放，无法继续使用",
            "请确保在资源释放后不再使用它")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ResourceDisposedError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请确保在资源释放后不再使用它")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源 ID</param>
    public ResourceDisposedError(SourcePosition position, string resourceType, int resourceId)
        : base(
            position,
            ErrorCode,
            $"{resourceType} {resourceId} 已被释放，无法继续使用",
            "请确保在资源释放后不再使用它")
    {
    }
}
