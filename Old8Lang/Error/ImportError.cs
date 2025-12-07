using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 导入错误
/// </summary>
public class ImportError : Old8Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="moduleName">模块名称</param>
    public ImportError(IOldLangTree node, string moduleName) 
        : base(
            "IMPORT_ERROR", 
            $"无法导入模块 '{moduleName}'",
            node,
            "请检查模块名称是否正确，或者模块是否存在")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="message">错误信息</param>
    public ImportError(IOldLangTree node, string moduleName, string message) 
        : base(
            "IMPORT_ERROR", 
            $"无法导入模块 '{moduleName}'：{message}",
            node,
            "请检查模块名称是否正确，或者模块是否存在")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="moduleName">模块名称</param>
    public ImportError(SourcePosition position, string moduleName) 
        : base(
            "IMPORT_ERROR", 
            $"无法导入模块 '{moduleName}'",
            position,
            null,
            "请检查模块名称是否正确，或者模块是否存在")
    {}
}

/// <summary>
/// 重复名称错误
/// </summary>
public class DuplicateNameError : Old8Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="name">重复的名称</param>
    /// <param name="type">名称类型（如"变量"、"函数"、"类"等）</param>
    public DuplicateNameError(IOldLangTree node, string name, string type) 
        : base(
            "DUPLICATE_NAME_ERROR", 
            $"{type} '{name}' 已被定义",
            node,
            "请使用不同的名称，或者删除重复的定义")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="name">重复的名称</param>
    /// <param name="type">名称类型（如"变量"、"函数"、"类"等）</param>
    public DuplicateNameError(SourcePosition position, string name, string type) 
        : base(
            "DUPLICATE_NAME_ERROR", 
            $"{type} '{name}' 已被定义",
            position,
            null,
            "请使用不同的名称，或者删除重复的定义")
    {}
}