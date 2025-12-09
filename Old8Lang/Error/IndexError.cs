using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 索引越界错误
/// </summary>
public class IndexError : RuntimeError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="index">索引值</param>
    /// <param name="length">长度</param>
    public IndexError(IOldLangTree node, int index, int length) 
        : base(
            node, 
            $"索引越界: 索引 {index} 超出范围 [0, {length - 1}]",
            "请检查索引值是否在有效范围内")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public IndexError(IOldLangTree node, string message) 
        : base(
            node, 
            message,
            "请检查索引相关操作")
    {}
}