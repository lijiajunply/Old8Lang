using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 文件不存在错误
/// </summary>
public class FileNotFoundError : RuntimeError
{
    /// <summary>
    /// 文件不存在错误代码
    /// </summary>
    public new const string ErrorCode = "FILE_NOT_FOUND_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="filePath">文件路径</param>
    public FileNotFoundError(IOldLangTree node, string filePath)
        : base(
            node,
            ErrorCode,
            $"文件不存在: '{filePath}'",
            "请检查文件路径是否正确，以及文件是否存在")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="filePath">文件路径</param>
    public FileNotFoundError(SourcePosition position, string filePath)
        : base(
            position,
            ErrorCode,
            $"文件不存在: '{filePath}'",
            "请检查文件路径是否正确，以及文件是否存在")
    {
    }
}
