using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// IO 错误基类
/// </summary>
public class IOError : RuntimeError
{
    /// <summary>
    /// IO 错误代码
    /// </summary>
    public new const string ErrorCode = "IO_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public IOError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查文件或IO操作是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected IOError(IOldLangTree node, string errorCode, string message, string suggestion)
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
    public IOError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查文件或IO操作是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected IOError(SourcePosition position, string errorCode, string message, string suggestion)
        : base(
            position,
            errorCode,
            message,
            suggestion)
    {
    }
}

/// <summary>
/// 文件读取错误
/// </summary>
public class FileReadError : IOError
{
    /// <summary>
    /// 文件读取错误代码
    /// </summary>
    public new const string ErrorCode = "FILE_READ_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="filePath">文件路径</param>
    public FileReadError(IOldLangTree node, string filePath)
        : base(
            node,
            ErrorCode,
            $"无法读取文件: '{filePath}'",
            "请检查文件是否存在，以及是否有读取权限")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="reason">失败原因</param>
    public FileReadError(IOldLangTree node, string filePath, string reason)
        : base(
            node,
            ErrorCode,
            $"无法读取文件 '{filePath}': {reason}",
            "请检查文件是否存在，以及是否有读取权限")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="filePath">文件路径</param>
    public FileReadError(SourcePosition position, string filePath)
        : base(
            position,
            ErrorCode,
            $"无法读取文件: '{filePath}'",
            "请检查文件是否存在，以及是否有读取权限")
    {
    }
}

/// <summary>
/// 文件写入错误
/// </summary>
public class FileWriteError : IOError
{
    /// <summary>
    /// 文件写入错误代码
    /// </summary>
    public new const string ErrorCode = "FILE_WRITE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="filePath">文件路径</param>
    public FileWriteError(IOldLangTree node, string filePath)
        : base(
            node,
            ErrorCode,
            $"无法写入文件: '{filePath}'",
            "请检查是否有写入权限，以及磁盘空间是否充足")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="reason">失败原因</param>
    public FileWriteError(IOldLangTree node, string filePath, string reason)
        : base(
            node,
            ErrorCode,
            $"无法写入文件 '{filePath}': {reason}",
            "请检查是否有写入权限，以及磁盘空间是否充足")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="filePath">文件路径</param>
    public FileWriteError(SourcePosition position, string filePath)
        : base(
            position,
            ErrorCode,
            $"无法写入文件: '{filePath}'",
            "请检查是否有写入权限，以及磁盘空间是否充足")
    {
    }
}

/// <summary>
/// 网络错误基类
/// </summary>
public class NetworkError : IOError
{
    /// <summary>
    /// 网络错误代码
    /// </summary>
    public new const string ErrorCode = "NETWORK_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public NetworkError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查网络连接是否正常")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected NetworkError(IOldLangTree node, string errorCode, string message, string suggestion)
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
    public NetworkError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查网络连接是否正常")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected NetworkError(SourcePosition position, string errorCode, string message, string suggestion)
        : base(
            position,
            errorCode,
            message,
            suggestion)
    {
    }
}

/// <summary>
/// 连接错误
/// </summary>
public class ConnectionError : NetworkError
{
    /// <summary>
    /// 连接错误代码
    /// </summary>
    public new const string ErrorCode = "CONNECTION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    public ConnectionError(IOldLangTree node, string host, int port)
        : base(
            node,
            ErrorCode,
            $"无法连接到 {host}:{port}",
            "请检查主机地址和端口是否正确，以及目标服务是否可用")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="url">URL地址</param>
    public ConnectionError(IOldLangTree node, string url)
        : base(
            node,
            ErrorCode,
            $"无法连接到 {url}",
            "请检查URL是否正确，以及目标服务是否可用")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <param name="reason">失败原因</param>
    public ConnectionError(IOldLangTree node, string host, int port, string reason)
        : base(
            node,
            ErrorCode,
            $"无法连接到 {host}:{port}: {reason}",
            "请检查主机地址和端口是否正确，以及目标服务是否可用")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    public ConnectionError(SourcePosition position, string host, int port)
        : base(
            position,
            ErrorCode,
            $"无法连接到 {host}:{port}",
            "请检查主机地址和端口是否正确，以及目标服务是否可用")
    {
    }
}

/// <summary>
/// 连接超时错误
/// </summary>
public class ConnectionTimeoutError : NetworkError
{
    /// <summary>
    /// 连接超时错误代码
    /// </summary>
    public new const string ErrorCode = "CONNECTION_TIMEOUT_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="host">主机地址</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public ConnectionTimeoutError(IOldLangTree node, string host, int timeoutMs)
        : base(
            node,
            ErrorCode,
            $"连接到 {host} 超时 ({timeoutMs}ms)",
            "请检查网络连接，或增加超时时间")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ConnectionTimeoutError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查网络连接，或增加超时时间")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="host">主机地址</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public ConnectionTimeoutError(SourcePosition position, string host, int timeoutMs)
        : base(
            position,
            ErrorCode,
            $"连接到 {host} 超时 ({timeoutMs}ms)",
            "请检查网络连接，或增加超时时间")
    {
    }
}

/// <summary>
/// HTTP 错误
/// </summary>
public class HttpError : NetworkError
{
    /// <summary>
    /// HTTP 错误代码
    /// </summary>
    public new const string ErrorCode = "HTTP_ERROR";

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="url">请求URL</param>
    public HttpError(IOldLangTree node, int statusCode, string url)
        : base(
            node,
            ErrorCode,
            $"HTTP 请求失败: {url} 返回状态码 {statusCode}",
            GetSuggestionForStatusCode(statusCode))
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="url">请求URL</param>
    /// <param name="message">错误信息</param>
    public HttpError(IOldLangTree node, int statusCode, string url, string message)
        : base(
            node,
            ErrorCode,
            $"HTTP 请求失败: {url} 返回状态码 {statusCode} - {message}",
            GetSuggestionForStatusCode(statusCode))
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="url">请求URL</param>
    public HttpError(SourcePosition position, int statusCode, string url)
        : base(
            position,
            ErrorCode,
            $"HTTP 请求失败: {url} 返回状态码 {statusCode}",
            GetSuggestionForStatusCode(statusCode))
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// 根据状态码获取建议
    /// </summary>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <returns>建议信息</returns>
    private static string GetSuggestionForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "请检查请求参数是否正确",
            401 => "请检查认证信息是否正确",
            403 => "请检查是否有访问权限",
            404 => "请检查URL是否正确",
            500 => "服务器内部错误，请稍后重试",
            502 => "网关错误，请检查服务是否正常",
            503 => "服务不可用，请稍后重试",
            504 => "网关超时，请检查网络连接",
            _ => "请检查请求是否正确"
        };
    }
}

/// <summary>
/// DNS 解析错误
/// </summary>
public class DnsResolutionError : NetworkError
{
    /// <summary>
    /// DNS 解析错误代码
    /// </summary>
    public new const string ErrorCode = "DNS_RESOLUTION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="hostname">主机名</param>
    public DnsResolutionError(IOldLangTree node, string hostname)
        : base(
            node,
            ErrorCode,
            $"无法解析主机名: '{hostname}'",
            "请检查主机名是否正确，以及DNS服务是否正常")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="hostname">主机名</param>
    public DnsResolutionError(SourcePosition position, string hostname)
        : base(
            position,
            ErrorCode,
            $"无法解析主机名: '{hostname}'",
            "请检查主机名是否正确，以及DNS服务是否正常")
    {
    }
}

/// <summary>
/// Socket 错误
/// </summary>
public class SocketError : NetworkError
{
    /// <summary>
    /// Socket 错误代码
    /// </summary>
    public new const string ErrorCode = "SOCKET_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public SocketError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            $"Socket 错误: {message}",
            "请检查网络连接和Socket配置")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public SocketError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            $"Socket 错误: {message}",
            "请检查网络连接和Socket配置")
    {
    }
}
