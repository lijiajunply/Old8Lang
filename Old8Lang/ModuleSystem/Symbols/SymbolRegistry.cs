using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;

namespace Old8Lang.ModuleSystem.Symbols;

/// <summary>
/// 符号注册器 - 负责将符号注册到变量管理器的作用域中
/// </summary>
public class SymbolRegistry
{
    /// <summary>
    /// 将符号注册到当前作用域
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="symbols">要注册的符号字典</param>
    public void RegisterSymbols(VariateManager manager, Dictionary<string, LangValueType> symbols)
    {
        var currentScope = manager.Scopes[^1];

        foreach (var (name, value) in symbols)
        {
            currentScope[name] = value;
        }
    }

    /// <summary>
    /// 将符号注册到指定作用域
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="symbols">要注册的符号字典</param>
    /// <param name="scopeIndex">作用域索引（负数表示从末尾开始计数）</param>
    public void RegisterSymbolsToScope(
        VariateManager manager,
        Dictionary<string, LangValueType> symbols,
        int scopeIndex = -1)
    {
        var actualIndex = scopeIndex < 0 ? manager.Scopes.Count + scopeIndex : scopeIndex;

        if (actualIndex < 0 || actualIndex >= manager.Scopes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(scopeIndex), "作用域索引超出范围");
        }

        var targetScope = manager.Scopes[actualIndex];

        foreach (var (name, value) in symbols)
        {
            targetScope[name] = value;
        }
    }

    /// <summary>
    /// 将符号注册到父作用域（用于命名导入）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="symbols">要注册的符号字典</param>
    public void RegisterSymbolsToParentScope(VariateManager manager, Dictionary<string, LangValueType> symbols)
    {
        if (manager.Scopes.Count < 2)
        {
            throw new InvalidOperationException("当前作用域没有父作用域");
        }

        RegisterSymbolsToScope(manager, symbols, -2);
    }

    /// <summary>
    /// 注册模块对象到当前作用域
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="moduleName">模块名称（作为变量名）</param>
    /// <param name="moduleObject">模块对象</param>
    public void RegisterModule(VariateManager manager, string moduleName, LangValueType moduleObject)
    {
        manager.Scopes[^1][moduleName] = moduleObject;
    }

    /// <summary>
    /// 注册带别名的符号
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="symbols">符号字典</param>
    /// <param name="aliases">别名映射（key: 原名, value: 别名）</param>
    public void RegisterSymbolsWithAliases(
        VariateManager manager,
        Dictionary<string, LangValueType> symbols,
        Dictionary<string, string> aliases)
    {
        var currentScope = manager.Scopes[^1];

        foreach (var (originalName, value) in symbols)
        {
            // 如果有别名，使用别名；否则使用原名
            var name = aliases.GetValueOrDefault(originalName, originalName);
            currentScope[name] = value;
        }
    }

    /// <summary>
    /// 检查符号是否已存在
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="symbolName">符号名称</param>
    /// <param name="searchGlobal">是否搜索全局作用域</param>
    /// <returns>是否存在</returns>
    public bool SymbolExists(VariateManager manager, string symbolName, bool searchGlobal = false)
    {
        if (searchGlobal)
        {
            // 搜索所有作用域
            return manager.Scopes.Any(scope => scope.ContainsKey(symbolName));
        }
        else
        {
            // 只搜索当前作用域
            return manager.Scopes[^1].ContainsKey(symbolName);
        }
    }

    /// <summary>
    /// 获取符号冲突列表
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="symbolNames">要检查的符号名称列表</param>
    /// <returns>冲突的符号名称列表</returns>
    public List<string> GetSymbolConflicts(VariateManager manager, IEnumerable<string> symbolNames)
    {
        var conflicts = new List<string>();
        var currentScope = manager.Scopes[^1];

        foreach (var symbolName in symbolNames)
        {
            if (currentScope.ContainsKey(symbolName))
            {
                conflicts.Add(symbolName);
            }
        }

        return conflicts;
    }
}
