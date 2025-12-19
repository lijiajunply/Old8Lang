using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 模块对象接口，定义所有模块对象应该实现的基本功能
/// </summary>
public interface IModuleObject
{
    /// <summary>
    /// 模块名称
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// 模块是否已加载
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// 模块加载状态
    /// </summary>
    ModuleLoadingState LoadingState { get; }

    /// <summary>
    /// 获取模块中的符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值，如果不存在返回null</returns>
    LangValueType? GetSymbol(string symbolName);

    /// <summary>
    /// 检查模块是否包含指定符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>是否包含符号</returns>
    bool HasSymbol(string symbolName);

    /// <summary>
    /// 获取模块中所有的导出符号名称
    /// </summary>
    /// <returns>符号名称列表</returns>
    IEnumerable<string> GetExportedSymbols();

    /// <summary>
    /// 强制加载模块（如果是懒加载）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    void EnsureLoaded(VariateManager manager);
}

/// <summary>
/// 模块加载状态枚举
/// </summary>
public enum ModuleLoadingState
{
    /// <summary>
    /// 未加载
    /// </summary>
    NotLoaded,

    /// <summary>
    /// 正在加载
    /// </summary>
    Loading,

    /// <summary>
    /// 已加载
    /// </summary>
    Loaded,

    /// <summary>
    /// 加载失败
    /// </summary>
    LoadFailed
}