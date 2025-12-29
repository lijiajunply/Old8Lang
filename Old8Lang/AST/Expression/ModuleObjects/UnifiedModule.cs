using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.Symbols;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 统一模块对象（重构版本）- 集成所有模块功能的单一实现
/// 支持懒加载、即时加载、选择性导入等多种模式
/// 性能优化：使用双字典提升符号查找效率，使用新的模块服务架构
/// 现在继承自 ImportInfo，可以被存储到 ImportInfos 列表中
/// </summary>
public class UnifiedModule(
    string moduleName,
    VariateManager manager,
    ModuleLoadMode loadMode = ModuleLoadMode.Lazy,
    SourcePosition position = default
) : ImportInfo(position), IModuleValueType
{
    // 符号存储 - 使用双字典优化查找性能
    private readonly Dictionary<string, LangValueType> Symbols = new();
    private readonly Dictionary<string, LangValueType> CaseInsensitiveSymbols =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Lock LoadLock = new();
    private readonly List<string> SelectedSymbols = [];
    private ModuleLoadingState _loadingState = ModuleLoadingState.NotLoaded;
    private Exception? _loadException;

    // 服务依赖
    private static readonly ModuleLoader ModuleLoaderInstance = new();
    private static readonly SymbolExtractor SymbolExtractorInstance = new();

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
    public bool IsLoaded => _loadingState == ModuleLoadingState.Loaded;

    /// <summary>
    /// 模块加载状态
    /// </summary>
    public ModuleLoadingState LoadingState => _loadingState;

    /// <summary>
    /// 加载异常（如果有）
    /// </summary>
    public Exception? LoadException => _loadException;

    /// <summary>
    /// 设置选择性导入的符号列表
    /// </summary>
    /// <param name="symbolNames">要导入的符号名称列表</param>
    public void SetSelectedSymbols(IEnumerable<string> symbolNames)
    {
        SelectedSymbols.AddRange(symbolNames);
    }

    /// <summary>
    /// 获取模块中的符号（优化版本 - O(1) 查找）
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值，如果不存在返回null</returns>
    public LangValueType? GetSymbol(string symbolName)
    {
        EnsureLoaded();

        // O(1) 精确查找
        if (Symbols.TryGetValue(symbolName, out var symbol))
        {
            return symbol;
        }

        // O(1) 大小写不敏感查找
        if (CaseInsensitiveSymbols.TryGetValue(symbolName, out symbol))
        {
            return symbol;
        }

        return null;
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
        return Symbols.Keys;
    }

    /// <summary>
    /// 强制加载模块（包含完整的状态管理）
    /// </summary>
    /// <param name="variateManager">变量管理器</param>
    public void EnsureLoaded(VariateManager? variateManager = null)
    {
        if (_loadingState == ModuleLoadingState.Loaded)
        {
            return;
        }

        // 如果已经失败，抛出之前的异常
        if (_loadingState == ModuleLoadingState.LoadFailed && _loadException != null)
        {
            throw new ImportError(this, ModuleName, $"模块之前加载失败: {_loadException.Message}");
        }

        lock (LoadLock)
        {
            // 双重检查锁定
            if (_loadingState == ModuleLoadingState.Loaded)
            {
                return;
            }

            if (_loadingState == ModuleLoadingState.Loading)
            {
                // 防止循环加载
                throw new ImportError(this, ModuleName, "检测到循环加载");
            }

            // 设置加载中状态
            _loadingState = ModuleLoadingState.Loading;

            try
            {
                LoadModuleInternal(variateManager);
                _loadingState = ModuleLoadingState.Loaded;
            }
            catch (Exception ex)
            {
                _loadingState = ModuleLoadingState.LoadFailed;
                _loadException = ex;
                throw;
            }
        }
    }

    /// <summary>
    /// 处理模块成员访问（性能优化版本）
    /// </summary>
    /// <param name="dotExpression">点表达式</param>
    /// <param name="currentManager">当前变量管理器</param>
    /// <returns>符号值</returns>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager currentManager)
    {
        EnsureLoaded(currentManager);

        return dotExpression switch
        {
            LangId langId => HandleSymbolAccess(langId),
            Instance instance => HandleFunctionCall(instance, currentManager),
            _ => throw new AttributeError(this, dotExpression.ToString() ?? "", ModuleName)
        };
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
            ModuleLoadMode.Selective => SelectedSymbols.Count > 0
                ? $"selective({SelectedSymbols.Count})"
                : "selective",
            _ => "unknown"
        };

        var status = _loadingState switch
        {
            ModuleLoadingState.Loaded => $"{Symbols.Count} symbols",
            ModuleLoadingState.Loading => "loading...",
            ModuleLoadingState.LoadFailed => "load failed",
            _ => "unloaded"
        };

        return $"<module {ModuleName} ({mode}, {status})>";
    }

    /// <summary>
    /// 创建即时加载模块
    /// </summary>
    public static UnifiedModule CreateEager(string moduleName, VariateManager manager,
        SourcePosition position = default)
    {
        var module = new UnifiedModule(moduleName, manager, ModuleLoadMode.Eager, position);
        // 即时加载
        module.EnsureLoaded(manager);
        return module;
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

        // 填充符号字典
        foreach (var (name, value) in symbols)
        {
            module.Symbols[name] = value;
            module.CaseInsensitiveSymbols[name] = value;
        }

        module._loadingState = ModuleLoadingState.Loaded;
        return module;
    }

    #region Private Methods

    /// <summary>
    /// 内部加载逻辑（使用新的服务架构）
    /// </summary>
    private void LoadModuleInternal(VariateManager? manager)
    {
        manager ??= new VariateManager();

        // 1. 解析模块路径
        var resolver = new ModuleResolver();
        var resolutionResult = resolver.ResolveModule(ModuleName, manager.Path, manager);

        if (!resolutionResult.IsSuccess || resolutionResult.ResolvedPath == null)
        {
            throw new ImportError(this, ModuleName, resolutionResult.AttemptedPaths);
        }

        // 2. 加载模块
        var loadResult = ModuleLoaderInstance.LoadModule(resolutionResult.ResolvedPath, manager);

        if (!loadResult.IsSuccess || loadResult.Block == null)
        {
            throw new ImportError(this, ModuleName,
                loadResult.Error?.Message ?? "模块加载失败");
        }

        // 3. 执行模块代码（直接在当前作用域执行，不创建临时作用域）
        // 这样函数可以访问模块中的变量
        loadResult.Block.Run(manager);

        // 4. 提取符号
        var moduleBaseName = Path.GetFileNameWithoutExtension(ModuleName);
        var selectedSymbolList = LoadMode == ModuleLoadMode.Selective && SelectedSymbols.Count > 0
            ? SelectedSymbols
            : null;

        var extractedSymbols = SymbolExtractorInstance.ExtractSymbols(
            manager,
            moduleBaseName,
            selectedSymbolList
        );

        // 5. 填充符号字典（双字典优化）
        foreach (var (name, value) in extractedSymbols)
        {
            Symbols[name] = value;
            CaseInsensitiveSymbols[name] = value;
        }
    }

    /// <summary>
    /// 处理符号访问（优化版本 - O(1) 查找，不回退到全局作用域）
    /// </summary>
    private LangValueType HandleSymbolAccess(LangId langId)
    {
        var symbolName = langId.IdName;
        var symbol = GetSymbol(symbolName);

        if (symbol == null)
        {
            throw new AttributeError(this, symbolName, ModuleName);
        }

        return symbol;
    }

    /// <summary>
    /// 处理函数调用
    /// </summary>
    private LangValueType HandleFunctionCall(Instance instance, VariateManager currentManager)
    {
        var functionName = instance.Id.IdName;
        var func = GetSymbol(functionName);

        if (func is FuncLangValue funcValue)
        {
            return funcValue.Run(currentManager, instance.Ids);
        }

        if (func is AsyncFuncLangValue asyncFuncValue)
        {
            return asyncFuncValue.RunAsync(currentManager, instance.Ids);
        }

        throw new AttributeError(this, functionName, ModuleName);
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("UnifiedModule 暂不支持 Visitor 模式访问");
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
