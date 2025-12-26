using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

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

/// <summary>
/// 基础模块接口，定义模块的核心属性
/// </summary>
public interface IModule
{
    /// <summary>
    /// 模块名称
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// 模块加载状态
    /// </summary>
    ModuleLoadingState LoadingState { get; }

    /// <summary>
    /// 模块是否已加载
    /// </summary>
    bool IsLoaded { get; }
}

/// <summary>
/// 符号提供者接口，定义模块符号管理功能
/// </summary>
public interface ISymbolProvider
{
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
}

/// <summary>
/// 可加载接口，定义模块加载功能
/// </summary>
public interface ILoadable
{
    /// <summary>
    /// 强制加载模块（如果是懒加载）
    /// </summary>
    /// <param name="variateManager">变量管理器</param>
    void EnsureLoaded(VariateManager variateManager);
}

/// <summary>
/// 完整的模块对象接口，组合所有模块功能
/// </summary>
public interface IModuleObject : IModule, ISymbolProvider, ILoadable
{
    // 继承所有基础接口的功能，无需额外定义
}

/// <summary>
/// 模块值类型接口，用于需要作为值使用的模块对象
/// </summary>
public interface IModuleValueType : IModuleObject
{
    /// <summary>
    /// 处理模块成员访问
    /// </summary>
    /// <param name="dotExpression">点表达式</param>
    /// <param name="currentManager">当前变量管理器</param>
    /// <returns>符号值</returns>
    LangValueType Dot(LangExpression dotExpression, VariateManager currentManager);
}

/// <summary>
/// 模块包装器接口，用于延迟加载等特殊场景
/// </summary>
public interface IModuleWrapper : IModuleValueType
{
    /// <summary>
    /// 获取被包装的模块对象
    /// </summary>
    /// <returns>被包装的模块对象</returns>
    IModuleObject? GetWrappedModule();

    /// <summary>
    /// 是否已加载
    /// </summary>
    bool IsWrapperLoaded { get; }
}