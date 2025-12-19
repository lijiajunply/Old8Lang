using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 统一模块对象 - 集成所有模块功能的单一实现
/// 支持懒加载、即时加载、选择性导入等多种模式
/// </summary>
public class UnifiedModule(
    string moduleName,
    VariateManager manager,
    ModuleLoadMode loadMode = ModuleLoadMode.Lazy,
    SourcePosition position = default
) : LangValueType(position), IModuleValueType
{
    private readonly Dictionary<string, LangValueType> _symbols = new();
    private readonly object _loadLock = new();
    private readonly List<string> _selectedSymbols = new();
    private bool _isLoaded;

    /// <summary>
    /// 模块加载模式
    /// </summary>
    public ModuleLoadMode LoadMode { get; } = loadMode;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; } = moduleName;

    /// <summary>
    /// 模块是否已加载
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// 模块加载状态
    /// </summary>
    public ModuleLoadingState LoadingState => _isLoaded ? ModuleLoadingState.Loaded : ModuleLoadingState.NotLoaded;

    /// <summary>
    /// 设置选择性导入的符号列表
    /// </summary>
    /// <param name="symbolNames">要导入的符号名称列表</param>
    public void SetSelectedSymbols(IEnumerable<string> symbolNames)
    {
        _selectedSymbols.AddRange(symbolNames);
    }

    /// <summary>
    /// 获取模块中的符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值，如果不存在返回null</returns>
    public LangValueType? GetSymbol(string symbolName)
    {
        EnsureLoaded();
        _symbols.TryGetValue(symbolName, out var symbol);
        return symbol;
    }

    /// <summary>
    /// 检查模块是否包含指定符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>是否包含符号</returns>
    public bool HasSymbol(string symbolName)
    {
        return GetSymbol(symbolName) != null;
    }

    /// <summary>
    /// 获取模块中所有的导出符号名称
    /// </summary>
    /// <returns>符号名称列表</returns>
    public IEnumerable<string> GetExportedSymbols()
    {
        EnsureLoaded();
        return _symbols.Keys;
    }

    /// <summary>
    /// 强制加载模块
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public void EnsureLoaded(VariateManager? manager = null)
    {
        if (!_isLoaded)
        {
            lock (_loadLock)
            {
                if (!_isLoaded)
                {
                    LoadModuleInternal(manager);
                }
            }
        }
    }

    /// <summary>
    /// 处理模块成员访问
    /// </summary>
    /// <param name="dotExpression">点表达式</param>
    /// <param name="currentManager">当前变量管理器</param>
    /// <returns>符号值</returns>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        EnsureLoaded(currentManager);

        switch (dotExpression)
        {
            case LangId langId:
                return HandleSymbolAccess(langId, currentManager);
            case Instance instance when instance.Id != null:
                return HandleFunctionCall(instance, currentManager);
            default:
                throw new AttributeError(this, dotExpression.ToString() ?? "", ModuleName);
        }
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var mode = LoadMode switch
        {
            ModuleLoadMode.Eager => "eager",
            ModuleLoadMode.Lazy => "lazy",
            ModuleLoadMode.Selective => _selectedSymbols.Count > 0
                ? $"selective({_selectedSymbols.Count})"
                : "selective",
            _ => "unknown"
        };

        var status = _isLoaded ? $"{_symbols.Count} symbols" : "unloaded";
        return $"<module {ModuleName} ({mode}, {status})>";
    }

    /// <summary>
    /// 创建即时加载模块
    /// </summary>
    public static UnifiedModule CreateEager(string moduleName, VariateManager manager, SourcePosition position = default)
    {
        return new UnifiedModule(moduleName, manager, ModuleLoadMode.Eager, position);
    }

    /// <summary>
    /// 创建懒加载模块
    /// </summary>
    public static UnifiedModule CreateLazy(string moduleName, VariateManager manager, SourcePosition position = default)
    {
        return new UnifiedModule(moduleName, manager, ModuleLoadMode.Lazy, position);
    }

    /// <summary>
    /// 创建选择性导入模块
    /// </summary>
    public static UnifiedModule CreateSelective(
        string moduleName,
        IEnumerable<string> selectedSymbols,
        VariateManager manager,
        SourcePosition position = default)
    {
        var module = new UnifiedModule(moduleName, manager, ModuleLoadMode.Selective, position);
        module.SetSelectedSymbols(selectedSymbols);
        return module;
    }

    /// <summary>
    /// 从现有符号创建模块（用于标准库等）
    /// </summary>
    public static UnifiedModule FromSymbols(
        string moduleName,
        Dictionary<string, LangValueType> symbols,
        SourcePosition position = default)
    {
        var module = new UnifiedModule(moduleName, new VariateManager(), ModuleLoadMode.Eager, position);
        module._symbols.Clear();
        foreach (var kvp in symbols)
        {
            module._symbols[kvp.Key] = kvp.Value;
        }
        module._isLoaded = true;
        return module;
    }

    #region Private Methods

    private void LoadModuleInternal(VariateManager? manager)
    {
        try
        {
            if (LoadMode == ModuleLoadMode.Eager || LoadMode == ModuleLoadMode.Selective)
            {
                LoadModuleFromImport(manager);
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            throw new ImportError(this, ModuleName, ex.Message);
        }
    }

    private void LoadModuleFromImport(VariateManager? manager)
    {
        manager ??= new VariateManager();

        // 创建临时作用域
        var tempScope = new Dictionary<string, LangValueType>();
        manager.Scopes.Add(tempScope);

        try
        {
            // 执行导入
            var importStatement = new ImportStatement(ModuleName, Position);
            importStatement.Run(manager);

            // 提取符号
            ExtractSymbolsFromScope(tempScope);
        }
        finally
        {
            // 清理临时作用域
            if (manager.Scopes.Count > 1)
            {
                manager.Scopes.RemoveAt(manager.Scopes.Count - 1);
            }
        }
    }

    private void ExtractSymbolsFromScope(Dictionary<string, LangValueType> scope)
    {
        var moduleBaseName = Path.GetFileNameWithoutExtension(ModuleName);

        foreach (var (symbolName, symbolValue) in scope)
        {
            // 跳过模块自身引用
            if (string.Equals(symbolName, moduleBaseName, StringComparison.OrdinalIgnoreCase))
                continue;

            // 跳过其他模块对象
            if (symbolValue is IModuleObject)
                continue;

            // 如果是选择性导入，只添加指定的符号
            if (LoadMode == ModuleLoadMode.Selective && _selectedSymbols.Count > 0)
            {
                if (_selectedSymbols.Contains(symbolName))
                {
                    _symbols[symbolName] = symbolValue;
                }
            }
            else
            {
                _symbols[symbolName] = symbolValue;
            }
        }
    }

    private LangValueType HandleSymbolAccess(LangId langId, VariateManager currentManager)
    {
        var symbolName = langId.IdName;

        // 1. 尝试从模块符号中获取
        if (_symbols.TryGetValue(symbolName, out var symbol))
        {
            return symbol;
        }

        // 2. 大小写不敏感查找
        var caseInsensitiveMatch = _symbols.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, symbolName, StringComparison.OrdinalIgnoreCase));
        if (caseInsensitiveMatch.Value != null)
        {
            return caseInsensitiveMatch.Value;
        }

        // 3. 代理到全局作用域（保持兼容性）
        var globalSymbol = currentManager.GetValue(langId);
        if (globalSymbol != null)
        {
            return globalSymbol;
        }

        throw new AttributeError(this, symbolName, ModuleName);
    }

    private LangValueType HandleFunctionCall(Instance instance, VariateManager currentManager)
    {
        var functionName = instance.Id.IdName;
        var func = GetSymbol(functionName) ??
                   _symbols.FirstOrDefault(kvp =>
                       string.Equals(kvp.Key, functionName, StringComparison.OrdinalIgnoreCase)).Value;

        if (func is FuncLangValue funcValue)
        {
            return funcValue.Run(currentManager, instance.Ids);
        }

        // 代理到全局作用域查找函数
        var globalFunc = currentManager.GetValue(new LangId(functionName));
        if (globalFunc is FuncLangValue globalFuncValue)
        {
            return globalFuncValue.Run(currentManager, instance.Ids);
        }

        throw new AttributeError(this, functionName, ModuleName);
    }

    #endregion
}

/// <summary>
/// 模块加载模式枚举
/// </summary>
public enum ModuleLoadMode
{
    /// <summary>
    /// 即时加载 - 创建时立即加载
    /// </summary>
    Eager,

    /// <summary>
    /// 懒加载 - 首次访问时加载
    /// </summary>
    Lazy,

    /// <summary>
    /// 选择性加载 - 只加载指定的符号
    /// </summary>
    Selective
}