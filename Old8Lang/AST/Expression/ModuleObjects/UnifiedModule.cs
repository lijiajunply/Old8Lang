using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
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
    private readonly Dictionary<string, LangValueType> Symbols = new();
    private readonly Lock LoadLock = new();
    private readonly List<string> SelectedSymbols = [];
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
        SelectedSymbols.AddRange(symbolNames);
    }

    /// <summary>
    /// 获取模块中的符号
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <returns>符号值，如果不存在返回null</returns>
    public LangValueType? GetSymbol(string symbolName)
    {
        EnsureLoaded();
        Symbols.TryGetValue(symbolName, out var symbol);
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
        return Symbols.Keys;
    }

    /// <summary>
    /// 强制加载模块
    /// </summary>
    /// <param name="variateManager">变量管理器</param>
    public void EnsureLoaded(VariateManager? variateManager = null)
    {
        if (!_isLoaded)
        {
            lock (LoadLock)
            {
                if (!_isLoaded)
                {
                    LoadModuleInternal(variateManager);
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

        return dotExpression switch
        {
            LangId langId => HandleSymbolAccess(langId, currentManager),
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

        var status = _isLoaded ? $"{Symbols.Count} symbols" : "unloaded";
        return $"<module {ModuleName} ({mode}, {status})>";
    }

    /// <summary>
    /// 创建即时加载模块
    /// </summary>
    public static UnifiedModule CreateEager(string moduleName, VariateManager manager,
        SourcePosition position = default)
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
        module.Symbols.Clear();
        foreach (var kvp in symbols)
        {
            module.Symbols[kvp.Key] = kvp.Value;
        }

        module._isLoaded = true;
        return module;
    }

    #region Private Methods

    private void LoadModuleInternal(VariateManager? manager)
    {
        try
        {
            if (LoadMode == ModuleLoadMode.Eager || LoadMode == ModuleLoadMode.Selective ||
                LoadMode == ModuleLoadMode.Lazy)
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

        // 创建一个独立的临时 manager 来加载模块，避免污染当前作用域
        var tempManager = new VariateManager
        {
            LangInfo = manager.LangInfo,
            Path = manager.Path,
            Interpreter = manager.Interpreter
        };

        // 保存原始的 ImportInfosList，避免被模块导入污染
        var originalImportInfos = manager.ImportInfos.ToList();

        // 执行导入到临时 manager
        var importStatement = new ImportStatement(ModuleName, Position);
        importStatement.Run(tempManager);

        // 从临时 manager 提取符号（函数、类、变量等）
        ExtractSymbolsFromManager(tempManager);
    }

    /// <summary>
    /// 从变量管理器中提取所有符号
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void ExtractSymbolsFromManager(VariateManager manager)
    {
        var moduleBaseName = Path.GetFileNameWithoutExtension(ModuleName);

        // 1. 从作用域中提取变量和常量
        foreach (var scope in manager.Scopes)
        {
            foreach (var (symbolName, symbolValue) in scope)
            {
                // 跳过模块自身引用
                if (string.Equals(symbolName, moduleBaseName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 跳过其他模块对象
                if (symbolValue is IModuleObject)
                    continue;

                // 如果是选择性导入，只添加指定的符号
                if (LoadMode == ModuleLoadMode.Selective && SelectedSymbols.Count > 0)
                {
                    if (SelectedSymbols.Contains(symbolName))
                    {
                        Symbols[symbolName] = symbolValue;
                    }
                }
                else
                {
                    Symbols[symbolName] = symbolValue;
                }
            }
        }

        // 2. 从 ImportInfos 中提取函数和类
        foreach (var importInfo in manager.ImportInfos)
        {
            string? symbolName = null;

            switch (importInfo)
            {
                case FuncLangValue { Id: not null } func:
                    symbolName = func.Id.IdName;
                    break;
                case AsyncFuncLangValue { Id: not null } asyncFunc:
                    symbolName = asyncFunc.Id.IdName;
                    break;
                case TypeTemplate template:
                    symbolName = template.ClassName;
                    break;
                case NativeAnyLangValue nativeAny:
                    symbolName = nativeAny.RegisterName;
                    break;
                case NativeStaticAny staticAny:
                    symbolName = staticAny.ClassName;
                    break;
            }

            if (symbolName != null)
            {
                // 跳过模块自身引用
                if (string.Equals(symbolName, moduleBaseName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 如果是选择性导入，只添加指定的符号
                if (LoadMode == ModuleLoadMode.Selective && SelectedSymbols.Count > 0)
                {
                    if (SelectedSymbols.Contains(symbolName))
                    {
                        Symbols[symbolName] = importInfo;
                    }
                }
                else
                {
                    Symbols[symbolName] = importInfo;
                }
            }
        }
    }


    private LangValueType HandleSymbolAccess(LangId langId, VariateManager currentManager)
    {
        var symbolName = langId.IdName;

        // 1. 尝试从模块符号中获取
        if (Symbols.TryGetValue(symbolName, out var symbol))
        {
            return symbol;
        }

        // 2. 大小写不敏感查找
        var caseInsensitiveMatch = Symbols.FirstOrDefault(kvp =>
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
                   Symbols.FirstOrDefault(kvp =>
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