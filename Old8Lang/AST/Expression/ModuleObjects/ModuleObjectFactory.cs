using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 模块对象工厂，负责根据导入配置创建合适的模块对象
/// </summary>
public static class ModuleObjectFactory
{
    /// <summary>
    /// 创建模块对象
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="importSpecifiers">导入指定项</param>
    /// <param name="fromClause">是否为from子句</param>
    /// <param name="moduleAlias">模块别名</param>
    /// <param name="isLazy">是否懒加载</param>
    /// <param name="isSelective">是否选择性导入</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    /// <returns>创建的模块对象</returns>
    public static IModuleObject CreateModuleObject(
        string moduleName,
        List<ImportItem>? importSpecifiers,
        bool fromClause,
        string? moduleAlias,
        bool isLazy,
        bool isSelective,
        VariateManager manager,
        SourcePosition position = default)
    {
        // 去除模块名称中的引号
        var cleanModuleName = moduleName.Trim('"');

        // 1. 处理选择性导入
        if ((fromClause || isSelective) && importSpecifiers is { Count: > 0 })
        {
            return new SelectiveModuleObject(cleanModuleName, importSpecifiers, isLazy, manager, position);
        }

        // 2. 处理带别名的导入
        if (!string.IsNullOrEmpty(moduleAlias))
        {
            return CreateAliasedModuleObject(cleanModuleName, moduleAlias, isLazy, manager, position);
        }

        // 3. 处理普通的懒导入或即时导入
        if (isLazy)
        {
            return new LazyModuleObject(cleanModuleName, manager, position);
        }

        return new EagerModuleObject(cleanModuleName, manager, position);
    }

    /// <summary>
    /// 创建带别名的模块对象
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="alias">别名</param>
    /// <param name="isLazy">是否懒加载</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    /// <returns>创建的模块对象</returns>
    private static IModuleObject CreateAliasedModuleObject(
        string moduleName,
        string alias,
        bool isLazy,
        VariateManager manager,
        SourcePosition position)
    {
        // 对于带别名的导入，我们需要创建一个模块对象，但是会通过SimpleModuleObject来处理
        // 这是为了保持与现有代码的兼容性
        if (isLazy)
        {
            return new LazyModuleObject(moduleName, manager, position);
        }

        return new EagerModuleObject(moduleName, manager, position);
    }

    /// <summary>
    /// 创建简单的模块代理对象（用于替换SimpleModuleObject）
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源码位置</param>
    /// <returns>模块代理对象</returns>
    public static SimpleModuleObjectProxy CreateModuleProxy(
        string moduleName,
        VariateManager manager,
        SourcePosition position = default)
    {
        return new SimpleModuleObjectProxy(moduleName, manager, position);
    }

    /// <summary>
    /// 创建从符号字典构建的模块对象
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="symbols">符号字典</param>
    /// <param name="position">源码位置</param>
    /// <returns>创建的模块对象</returns>
    public static IModuleObject CreateModuleFromSymbols(
        string moduleName,
        Dictionary<string, LangValueType> symbols,
        SourcePosition position = default)
    {
        return new EagerModuleObject(moduleName, symbols, position);
    }
}