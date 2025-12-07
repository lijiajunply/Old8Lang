using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 名称错误（变量或函数未定义）
/// </summary>
public class NameError : Old8Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="name">名称</param>
    public NameError(IOldLangTree node, string name) 
        : base(
            "NAME_ERROR", 
            $"名称 '{name}' 未定义",
            node,
            "请检查名称拼写或是否已定义")
    {}
}