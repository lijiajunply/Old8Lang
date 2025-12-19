using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 统一模块工厂 - 替代所有分散的工厂类
/// 提供创建各种类型模块对象的单一入口
/// </summary>
public static class ModuleFactory
{
    /// <summary>
    /// 创建模块对象的通用方法
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="loadMode">加载模式</param>
    /// <param name="position">源码位置</param>
    /// <returns>模块对象</returns>
    public static UnifiedModule CreateModule(
        string moduleName,
        VariateManager manager,
        ModuleLoadMode loadMode = ModuleLoadMode.Lazy,
        SourcePosition position = default)
    {
        return new UnifiedModule(moduleName, manager, loadMode, position);
    }

    /// <summary>
    /// 创建即时加载模块
    /// </summary>
    public static UnifiedModule CreateEagerModule(
        string moduleName,
        VariateManager manager,
        SourcePosition position = default)
    {
        return UnifiedModule.CreateEager(moduleName, manager, position);
    }

    /// <summary>
    /// 创建懒加载模块
    /// </summary>
    public static UnifiedModule CreateLazyModule(
        string moduleName,
        VariateManager manager,
        SourcePosition position = default)
    {
        return UnifiedModule.CreateLazy(moduleName, manager, position);
    }

    /// <summary>
    /// 创建选择性导入模块
    /// </summary>
    public static UnifiedModule CreateSelectiveModule(
        string moduleName,
        List<string> selectedSymbols,
        VariateManager manager,
        SourcePosition position = default)
    {
        return UnifiedModule.CreateSelective(moduleName, selectedSymbols, manager, position);
    }

    /// <summary>
    /// 从现有符号创建模块（用于标准库等）
    /// </summary>
    public static UnifiedModule CreateModuleFromSymbols(
        string moduleName,
        Dictionary<string, LangValueType> symbols,
        SourcePosition position = default)
    {
        return UnifiedModule.FromSymbols(moduleName, symbols, position);
    }

    /// <summary>
    /// 创建模块值类型对象（兼容旧接口）
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="isLazy">是否懒加载</param>
    /// <param name="position">源码位置</param>
    /// <returns>模块值类型对象</returns>
    public static IModuleValueType CreateModuleValue(
        string moduleName,
        VariateManager manager,
        bool isLazy = true,
        SourcePosition position = default)
    {
        var loadMode = isLazy ? ModuleLoadMode.Lazy : ModuleLoadMode.Eager;
        return CreateModule(moduleName, manager, loadMode, position);
    }

    /// <summary>
    /// 为标准库创建预定义模块
    /// </summary>
    /// <param name="moduleName">标准库名称</param>
    /// <param name="position">源码位置</param>
    /// <returns>标准库模块对象</returns>
    public static IModuleValueType CreateStandardLibraryModule(
        string moduleName,
        SourcePosition position = default)
    {
        // 这里可以添加标准库的特殊逻辑
        // 目前返回一个基础模块，符号由StandardLibraryManager填充
        var manager = new VariateManager();
        return CreateModuleFromSymbols(moduleName, new Dictionary<string, LangValueType>(), position);
    }

    /// <summary>
    /// 创建模块代理（用于特殊场景）
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    /// <returns>模块代理对象</returns>
    public static IModuleValueType CreateModuleProxy(
        string moduleName,
        VariateManager manager,
        SourcePosition position = default)
    {
        // 对于简单的代理需求，我们使用即时加载的统一模块
        return CreateEagerModule(moduleName, manager, position);
    }
}

/// <summary>
/// 工厂配置选项
/// </summary>
public class FactoryOptions
{
    /// <summary>
    /// 默认加载模式
    /// </summary>
    public ModuleLoadMode DefaultLoadMode { get; set; } = ModuleLoadMode.Lazy;

    /// <summary>
    /// 是否启用符号缓存
    /// </summary>
    public bool EnableSymbolCache { get; set; } = true;

    /// <summary>
    /// 是否启用大小写不敏感查找
    /// </summary>
    public bool EnableCaseInsensitiveLookup { get; set; } = true;

    /// <summary>
    /// 默认工厂选项实例
    /// </summary>
    public static FactoryOptions Default { get; } = new();
}