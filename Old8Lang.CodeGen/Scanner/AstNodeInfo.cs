// Scanner/AstNodeInfo.cs
namespace Old8Lang.CodeGen.Scanner;

/// <summary>
/// AST 节点信息
/// </summary>
public class AstNodeInfo
{
    /// <summary>
    /// 类名（如：IfStatement）
    /// </summary>
    public string ClassName { get; set; } = "";

    /// <summary>
    /// 完整类型名（如：Old8Lang.AST.Statement.IfStatement）
    /// </summary>
    public string FullTypeName { get; set; } = "";

    /// <summary>
    /// 命名空间（如：Old8Lang.AST.Statement）
    /// </summary>
    public string Namespace { get; set; } = "";

    /// <summary>
    /// 节点类别（Statement / Expression / Value）
    /// </summary>
    public AstNodeCategory Category { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = "";
}

/// <summary>
/// AST 节点类别
/// </summary>
public enum AstNodeCategory
{
    /// <summary>
    /// 语句节点
    /// </summary>
    Statement,

    /// <summary>
    /// 表达式节点
    /// </summary>
    Expression,

    /// <summary>
    /// 值类型节点
    /// </summary>
    Value
}
