using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 模块对象基类，提供模块对象的通用功能实现
/// </summary>
public abstract class BaseModuleObject : LangValueType, IModuleObject
{
    private readonly Dictionary<string, LangValueType> Symbols = new();
    private readonly Lock LoadLock = new();
    private ModuleLoadingState _loadingState = ModuleLoadingState.NotLoaded;
    private Exception? LoadException;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="position">源码位置</param>
    protected BaseModuleObject(string moduleName, SourcePosition position = default)
        : base(position)
    {
        ModuleName = moduleName;
    }

    #region IModuleObject Implementation

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// 模块是否已加载
    /// </summary>
    public bool IsLoaded => _loadingState == ModuleLoadingState.Loaded;

    /// <summary>
    /// 模块加载状态
    /// </summary>
    public ModuleLoadingState LoadingState
    {
        get => _loadingState;
        protected set => _loadingState = value;
    }

    /// <summary>
    /// 获取模块中的符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值，如果不存在返回null</returns>
    public virtual LangValueType? GetSymbol(string symbolName)
    {
        EnsureLoadedInternal();
        Symbols.TryGetValue(symbolName, out var symbol);
        return symbol;
    }

    /// <summary>
    /// 检查模块是否包含指定符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>是否包含符号</returns>
    public virtual bool HasSymbol(string symbolName)
    {
        EnsureLoadedInternal();
        return Symbols.ContainsKey(symbolName);
    }

    /// <summary>
    /// 获取模块中所有的导出符号名称
    /// </summary>
    /// <returns>符号名称列表</returns>
    public virtual IEnumerable<string> GetExportedSymbols()
    {
        EnsureLoadedInternal();
        return Symbols.Keys;
    }

    /// <summary>
    /// 强制加载模块（如果是懒加载）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public void EnsureLoaded(VariateManager manager)
    {
        if (!IsLoaded)
        {
            lock (LoadLock)
            {
                if (!IsLoaded && _loadingState != ModuleLoadingState.Loading)
                {
                    PerformModuleLoad(manager);
                }
            }
        }
    }

    #endregion

    #region LangValueType Overrides

    /// <summary>
    /// 处理模块成员访问
    /// </summary>
    /// <param name="dotExpression">点表达式</param>
    /// <param name="currentManager">当前变量管理器</param>
    /// <returns>符号值</returns>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        if (dotExpression is LangId langId)
        {
            var symbolName = langId.IdName;

            // 支持大小写不敏感的查找
            var symbol = GetSymbol(symbolName) ?? GetSymbolIgnoreCase(symbolName);

            if (symbol != null)
            {
                return symbol;
            }

            throw new AttributeError(this, symbolName, ModuleName);
        }

        if (dotExpression is Instance instance)
        {
            // 处理函数调用：module.function(args)
            var functionName = instance.Id?.IdName;
            if (!string.IsNullOrEmpty(functionName))
            {
                var func = GetSymbol(functionName) ?? GetSymbolIgnoreCase(functionName);

                if (func is FuncLangValue funcValue)
                {
                    return funcValue.Run(currentManager, instance.Ids);
                }

                throw new AttributeError(this, functionName, ModuleName);
            }
        }

        throw new AttributeError(this, dotExpression.ToString() ?? "", ModuleName);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"<module {ModuleName} ({_loadingState})>";
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 添加符号到模块
    /// </summary>
    /// <param name="name">符号名称</param>
    /// <param name="value">符号值</param>
    protected void AddSymbol(string name, LangValueType value)
    {
        Symbols[name] = value;
    }

    /// <summary>
    /// 批量添加符号到模块
    /// </summary>
    /// <param name="symbols">符号字典</param>
    protected void AddSymbols(Dictionary<string, LangValueType> symbols)
    {
        foreach (var kvp in symbols)
        {
            Symbols[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 子类实现的模块加载逻辑
    /// </summary>
    /// <param name="manager">变量管理器</param>
    protected abstract void LoadModule(VariateManager manager);

    /// <summary>
    /// 加载失败时的处理
    /// </summary>
    /// <param name="exception">异常信息</param>
    protected void OnLoadFailed(Exception exception)
    {
        _loadingState = ModuleLoadingState.LoadFailed;
        LoadException = exception;
    }

    /// <summary>
    /// 加载成功时的处理
    /// </summary>
    protected void OnLoadSuccess()
    {
        _loadingState = ModuleLoadingState.Loaded;
        LoadException = null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 内部确保模块已加载（不加锁，用于已持有锁的情况）
    /// </summary>
    private void EnsureLoadedInternal()
    {
        if (!IsLoaded)
        {
            throw new InvalidOperationException($"Module '{ModuleName}' is not loaded. Call EnsureLoaded() first.");
        }
    }

    /// <summary>
    /// 大小写不敏感的符号查找
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值</returns>
    private LangValueType? GetSymbolIgnoreCase(string symbolName)
    {
        return Symbols.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, symbolName, StringComparison.OrdinalIgnoreCase)).Value;
    }

    /// <summary>
    /// 实际的模块加载逻辑
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void PerformModuleLoad(VariateManager manager)
    {
        try
        {
            _loadingState = ModuleLoadingState.Loading;
            LoadModule(manager);
            OnLoadSuccess();
        }
        catch (Exception ex)
        {
            OnLoadFailed(ex);
            throw new ImportError(this, ModuleName, ex.Message);
        }
    }

    #endregion
}