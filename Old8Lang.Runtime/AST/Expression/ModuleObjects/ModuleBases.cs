using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 简化的模块基类实现 - 修复版本
/// </summary>
public abstract class ModuleBase(string moduleName) : IModuleObject, IOldLangTree
{
    private readonly Dictionary<string, LangValueType> _symbols = new();
    private readonly Lock _loadLock = new();

    /// <summary>
    /// 源码位置
    /// </summary>
    public virtual SourcePosition Position => default;

    #region IModuleObject Implementation

    public string ModuleName { get; } = moduleName;
    public virtual bool IsLoaded => LoadingState == ModuleLoadingState.Loaded;
    public virtual ModuleLoadingState LoadingState { get; protected set; } = ModuleLoadingState.NotLoaded;

    public virtual LangValueType? GetSymbol(string symbolName)
    {
        EnsureLoadedInternal();
        _symbols.TryGetValue(symbolName, out var symbol);
        return symbol;
    }

    public virtual bool HasSymbol(string symbolName)
    {
        EnsureLoadedInternal();
        return _symbols.ContainsKey(symbolName);
    }

    public virtual IEnumerable<string> GetExportedSymbols()
    {
        EnsureLoadedInternal();
        return _symbols.Keys;
    }

    public virtual void EnsureLoaded(VariateManager variateManager)
    {
        if (!IsLoaded)
        {
            lock (_loadLock)
            {
                if (!IsLoaded && LoadingState != ModuleLoadingState.Loading)
                {
                    PerformModuleLoad(variateManager);
                }
            }
        }
    }

    #endregion

    #region Protected Methods

    protected void AddSymbol(string name, LangValueType value)
    {
        _symbols[name] = value;
    }

    protected abstract void LoadModule(VariateManager manager);

    protected virtual void OnLoadFailed(Exception exception)
    {
        LoadingState = ModuleLoadingState.LoadFailed;
    }

    protected virtual void OnLoadSuccess()
    {
        LoadingState = ModuleLoadingState.Loaded;
    }

    protected virtual LangValueType? GetSymbolIgnoreCase(string symbolName)
    {
        return _symbols.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, symbolName, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private void EnsureLoadedInternal()
    {
        if (!IsLoaded)
        {
            throw new InvalidOperationException($"Module '{ModuleName}' is not loaded. Call EnsureLoaded() first.");
        }
    }

    private void PerformModuleLoad(VariateManager manager)
    {
        try
        {
            LoadingState = ModuleLoadingState.Loading;
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

    public override string ToString()
    {
        return $"<module {ModuleName} ({LoadingState})>";
    }

    public abstract TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor);
}

/// <summary>
/// 简化的模块值基类实现 - 修复版本
/// </summary>
public abstract class ModuleValueBase(IModuleObject moduleObject, SourcePosition position = default)
    : LangValueType(position), IModuleValueType
{
    #region IModuleValueType Implementation

    public virtual string ModuleName => moduleObject.ModuleName;
    public virtual bool IsLoaded => moduleObject.IsLoaded;
    public virtual ModuleLoadingState LoadingState => moduleObject.LoadingState;

    public virtual LangValueType? GetSymbol(string symbolName) => moduleObject.GetSymbol(symbolName);
    public virtual bool HasSymbol(string symbolName) => moduleObject.HasSymbol(symbolName);
    public virtual IEnumerable<string> GetExportedSymbols() => moduleObject.GetExportedSymbols();

    public virtual void EnsureLoaded(VariateManager variateManager) => moduleObject.EnsureLoaded(variateManager);

    public abstract override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager);

    #endregion

    protected virtual LangValueType? GetSymbolIgnoreCase(string symbolName)
    {
        var allSymbols = GetExportedSymbols();
        foreach (var symbol in allSymbols)
        {
            if (string.Equals(symbol, symbolName, StringComparison.OrdinalIgnoreCase))
            {
                return GetSymbol(symbol);
            }
        }

        return null;
    }

    public override string ToString()
    {
        return moduleObject.ToString() ?? "";
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("ModuleBase 暂不支持 Visitor 模式访问");
    }
}