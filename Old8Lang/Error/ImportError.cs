using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 导入错误
/// </summary>
public class ImportError : RuntimeError
{
    /// <summary>
    /// 导入错误代码
    /// </summary>
    public new const string ErrorCode = "IMPORT_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    public ImportError(IOldLangTree node, string moduleName) 
        : base(
            node, 
            ErrorCode,
            $"无法导入模块 '{moduleName}'",
            "请检查模块名称是否正确，或者模块是否存在")
    {}
    
    /// <summary>
    /// 构造函数，带详细错误信息
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="message">详细错误信息</param>
    public ImportError(IOldLangTree node, string moduleName, string message) 
        : base(
            node, 
            ErrorCode,
            $"无法导入模块 '{moduleName}'：{message}",
            "请检查模块名称是否正确，或者模块是否存在")
    {}
    
    /// <summary>
    /// 构造函数，使用位置信息
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    public ImportError(SourcePosition position, string moduleName) 
        : base(
            position, 
            ErrorCode,
            $"无法导入模块 '{moduleName}'",
            "请检查模块名称是否正确，或者模块是否存在")
    {}
}

/// <summary>
/// 重复名称错误
/// </summary>
public class DuplicateNameError : RuntimeError
{
    /// <summary>
    /// 重复名称错误代码
    /// </summary>
    public new const string ErrorCode = "DUPLICATE_NAME_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="name">重复定义的名称</param>
    /// <param name="type">名称类型（如"变量"、"函数"、"类"等）</param>
    public DuplicateNameError(IOldLangTree node, string name, string type) 
        : base(
            node, 
            ErrorCode,
            $"{type} '{name}' 已被定义",
            "请使用不同的名称，或者删除重复的定义")
    {}
    
    /// <summary>
    /// 构造函数，使用位置信息
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    /// <param name="name">重复定义的名称</param>
    /// <param name="type">名称类型（如"变量"、"函数"、"类"等）</param>
    public DuplicateNameError(SourcePosition position, string name, string type) 
        : base(
            position, 
            ErrorCode,
            $"{type} '{name}' 已被定义",
            "请使用不同的名称，或者删除重复的定义")
    {}
}