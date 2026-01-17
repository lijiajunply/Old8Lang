using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 统一模块工厂 - 替代所有分散的工厂类
/// 提供创建各种类型模块对象的单一入口
/// </summary>
public static class ModuleFactory
{
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
}