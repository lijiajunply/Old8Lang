using Old8Lang.AST;
using Old8Lang.ModuleSystem.Resolution;

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
    /// 无法导入的模块名称
    /// </summary>
    public string ModuleName { get; } 
    
    /// <summary>
    /// 尝试的文件路径列表
    /// </summary>
    public List<string> AttemptedPaths { get; } 

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="attemptedPaths">尝试的文件路径列表</param>
    public ImportError(IOldLangTree node, string moduleName, List<string>? attemptedPaths = null) 
        : base(
            node, 
            ErrorCode,
            BuildErrorMessage(moduleName, attemptedPaths ?? []),
            "请检查模块名称是否正确，或者模块是否存在")
    {
        ModuleName = moduleName;
        AttemptedPaths = attemptedPaths ?? [];
    }
    
    /// <summary>
    /// 构造函数，带详细错误信息
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="message">详细错误信息</param>
    /// <param name="attemptedPaths">尝试的文件路径列表</param>
    public ImportError(IOldLangTree node, string moduleName, string message, List<string>? attemptedPaths = null) 
        : base(
            node, 
            ErrorCode,
            BuildErrorMessage(moduleName, attemptedPaths ?? [], message),
            "请检查模块名称是否正确，或者模块是否存在")
    {
        ModuleName = moduleName;
        AttemptedPaths = attemptedPaths ?? [];
    }
    
    /// <summary>
    /// 构造函数，使用位置信息
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="attemptedPaths">尝试的文件路径列表</param>
    public ImportError(SourcePosition position, string moduleName, List<string>? attemptedPaths = null)
        : base(
            position,
            ErrorCode,
            BuildErrorMessage(moduleName, attemptedPaths ?? []),
            "请检查模块名称是否正确，或者模块是否存在")
    {
        ModuleName = moduleName;
        AttemptedPaths = attemptedPaths ?? [];
    }

    /// <summary>
    /// 构造函数，使用位置信息和自定义错误消息
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="message">详细错误信息</param>
    public ImportError(SourcePosition position, string moduleName, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查模块名称和导入的符号是否正确")
    {
        ModuleName = moduleName;
        AttemptedPaths = [];
    }

    /// <summary>
    /// 构造函数，使用 ModuleResolutionResult（推荐）
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="resolutionResult">模块解析结果</param>
    public ImportError(SourcePosition position, string moduleName, ModuleResolutionResult resolutionResult)
        : base(
            position,
            ErrorCode,
            resolutionResult.GetFriendlyErrorMessage(moduleName),
            "请检查模块名称和路径是否正确")
    {
        ModuleName = moduleName;
        AttemptedPaths = resolutionResult.AttemptedPaths;
    }

    /// <summary>
    /// 构造函数，使用 IOldLangTree 和 ModuleResolutionResult
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="resolutionResult">模块解析结果</param>
    public ImportError(IOldLangTree node, string moduleName, ModuleResolutionResult resolutionResult)
        : base(
            node,
            ErrorCode,
            resolutionResult.GetFriendlyErrorMessage(moduleName),
            "请检查模块名称和路径是否正确")
    {
        ModuleName = moduleName;
        AttemptedPaths = resolutionResult.AttemptedPaths;
    }
    
    /// <summary>
    /// 构造函数，用于循环依赖检测
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    /// <param name="moduleName">无法导入的模块名称</param>
    /// <param name="importStack">当前导入栈</param>
    public ImportError(SourcePosition position, string moduleName, Stack<string> importStack) 
        : base(
            position, 
            ErrorCode,
            BuildCircularDependencyMessage(moduleName, importStack),
            "请检查模块导入关系，避免循环依赖")
    {
        ModuleName = moduleName;
        AttemptedPaths = new List<string>();
    }
    
    /// <summary>
    /// 构建错误信息
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="attemptedPaths">尝试的路径列表</param>
    /// <param name="message">附加错误信息</param>
    /// <returns>格式化的错误信息</returns>
    private static string BuildErrorMessage(string moduleName, List<string> attemptedPaths, string? message = null)
    {
        var errorMsg = new List<string> {
            $"无法导入模块 '{moduleName}'" 
        };
        
        if (!string.IsNullOrEmpty(message))
        {
            errorMsg.Add($"  原因: {message}");
        }
        
        if (attemptedPaths != null && attemptedPaths.Count > 0)
        {
            errorMsg.Add("  尝试的路径:");
            foreach (var path in attemptedPaths)
            {
                errorMsg.Add($"    - {path}");
            }
        }
        
        return string.Join(Environment.NewLine, errorMsg);
    }
    
    /// <summary>
    /// 构建循环依赖错误信息
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="importStack">导入栈</param>
    /// <returns>格式化的循环依赖错误信息</returns>
    private static string BuildCircularDependencyMessage(string moduleName, Stack<string> importStack)
    {
        var stack = new Stack<string>(importStack);
        var dependencyChain = new List<string> { moduleName };
        
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            dependencyChain.Add(current);
            if (current == moduleName) break;
        }
        
        dependencyChain.Reverse();
        
        return $"循环依赖检测到: {string.Join(" -> ", dependencyChain)}";
    }
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