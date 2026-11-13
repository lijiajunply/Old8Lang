using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 名称错误（变量或函数未定义）
/// </summary>
public class NameError : RuntimeError
{
    /// <summary>
    /// 名称错误代码
    /// </summary>
    public new const string ErrorCode = "NAME_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="name">未定义的名称</param>
    public NameError(IOldLangTree node, string name) 
        : base(
            node, 
            ErrorCode,
            $"名称 '{name}' 未定义",
            "请检查名称拼写或是否已定义")
    {}

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    /// <param name="name">未定义的名称</param>
    public NameError(SourcePosition position, string name) 
        : base(
            position, 
            ErrorCode,
            $"名称 '{name}' 未定义",
            "请检查名称拼写或是否已定义")
    {}
}