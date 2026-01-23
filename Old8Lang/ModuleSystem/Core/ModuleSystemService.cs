using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.Symbols;
using Old8Lang.Interpreter;
using Old8Lang.Error;
using Old8Lang.AST.Statement;
using Old8Lang.StandardLibrary;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.AnyValues;

namespace Old8Lang.ModuleSystem.Core;

/// <summary>
/// 模块系统服务 - 提供统一的模块管理接口
/// 整合了解析、加载和符号管理功能
/// </summary>
public class ModuleSystemService
{
    private readonly ModuleResolver _resolver = new();
    private readonly ModuleLoader _loader = new();
    private readonly SymbolExtractor _symbolExtractor = new();
    private readonly SymbolRegistry _symbolRegistry = new();
    private readonly NetworkModuleLoader _networkLoader = new();

    /// <summary>
    /// 导入模块的完整流程
    /// </summary>
    public ModuleImportResult ImportModule(
        string moduleName,
        VariateManager manager,
        ImportOptions options)
    {
        var result = new ModuleImportResult { ModuleName = moduleName };

        try
        {
            // 1. 解析模块
            var resolution = _resolver.ResolveModule(moduleName, manager.Path, manager);
            result.ResolutionResult = resolution;

            if (!resolution.IsSuccess)
            {
                // 使用新的构造函数，提供详细的错误信息
                result.Error = new ImportError(default(SourcePosition), moduleName, resolution);
                return result;
            }

            // 2. 处理不同类型的模块
            switch (resolution.ModuleType)
            {
                case ModuleType.StandardLibrary:
                    return HandleStandardLibraryImport(moduleName, manager, options, result);

                case ModuleType.NetworkModule:
                    return HandleNetworkModuleImport(resolution.ResolvedPath!, manager, options, result);

                case ModuleType.LocalFile:
                case ModuleType.ThirdPartyPackage:
                case ModuleType.Submodule:
                    return HandleLocalModuleImport(resolution.ResolvedPath!, manager, options, result);

                default:
                    result.Error = new ImportError(default, moduleName, "未知的模块类型");
                    return result;
            }
        }
        catch (Exception ex)
        {
            result.Error = ex;
            return result;
        }
    }

    /// <summary>
    /// 处理标准库导入
    /// </summary>
    private ModuleImportResult HandleStandardLibraryImport(
        string moduleName,
        VariateManager manager,
        ImportOptions options,
        ModuleImportResult result)
    {
        if (StandardLibraryLoader.TryLoadStandardLibrary(moduleName, manager, out var stdModule))
        {
            result.ModuleObject = stdModule;
            result.IsSuccess = true;

            // 注册到作用域
            if (options.ModuleAlias is not null)
            {
                _symbolRegistry.RegisterModule(manager, options.ModuleAlias, stdModule);
            }
            else if (!options.IsFromClause)
            {
                _symbolRegistry.RegisterModule(manager, moduleName, stdModule);
            }
        }
        else
        {
            result.Error = new ImportError(default, moduleName, "标准库加载失败");
        }

        return result;
    }

    /// <summary>
    /// 处理网络模块导入
    /// </summary>
    private ModuleImportResult HandleNetworkModuleImport(
        string url,
        VariateManager manager,
        ImportOptions options,
        ModuleImportResult result)
    {
        // 下载模块
        var localPath = _networkLoader.DownloadModule(url);
        if (localPath is null)
        {
            result.Error = new ImportError(default, url, "网络模块下载失败");
            return result;
        }

        // 作为本地文件加载
        return HandleLocalModuleImport(localPath, manager, options, result);
    }

    /// <summary>
    /// 处理本地模块导入
    /// </summary>
    private ModuleImportResult HandleLocalModuleImport(
        string modulePath,
        VariateManager manager,
        ImportOptions options,
        ModuleImportResult result)
    {
        // 获取绝对路径用于循环依赖检测
        var absolutePath = Path.GetFullPath(modulePath);

        // 先检查循环依赖（在加载前检查）
        if (manager.ImportStack.Contains(absolutePath))
        {
            result.Error = new ImportError(default, modulePath, manager.ImportStack);
            return result;
        }

        // 加载模块（不会执行，只解析）
        var loadResult = _loader.LoadModule(modulePath, manager);
        if (!loadResult.IsSuccess || loadResult.Block is null)
        {
            result.Error = loadResult.Error ?? new ImportError(default, modulePath, "模块加载失败");
            return result;
        }

        result.Block = loadResult.Block;
        result.IsFromCache = loadResult.IsFromCache;

        // 如果从缓存加载，不需要再执行和提取符号，直接使用缓存的模块对象
        if (loadResult.IsFromCache && manager.Interpreter.ModuleCache.ContainsKey(absolutePath))
        {
            // 缓存的模块已经执行过，只需要注册符号
            if (options.ModuleAlias is not null)
            {
                var moduleName = Path.GetFileNameWithoutExtension(modulePath);
                var cachedSymbols = _symbolExtractor.ExtractSymbols(manager);
                var moduleObj = UnifiedModule.FromSymbols(moduleName, cachedSymbols);
                _symbolRegistry.RegisterModule(manager, options.ModuleAlias, moduleObj);
                result.ModuleObject = moduleObj;
                result.IsSuccess = true;
            }

            return result;
        }

        // 执行模块代码（需要在 ImportStack 保护下执行，防止循环依赖）
        manager.ImportStack.Push(absolutePath);
        try
        {
            // 执行模块并提取符号
            if (options.IsFromClause)
            {
                // 命名导入
                manager.AddChildren();
                try
                {
                    // 记录执行前的 ImportInfos
                    var importInfosBefore = manager.ImportInfos.ToList();

                    loadResult.Block.Run(manager);

                    // 找出新增的 ImportInfos（只属于当前模块的）
                    var newImportInfos = manager.ImportInfos.Except(importInfosBefore).ToList();

                    // 判断是否是通配符导入（没有指定导入符号列表）
                    var isWildcardImport = options.ImportSpecifiers is null || options.ImportSpecifiers.Count == 0;

                    // 如果是通配符导入，将作用域中的常量包装为 ConstantLangValue 并添加到 ImportInfos
                    if (isWildcardImport)
                    {
                        _symbolExtractor.WrapConstantsAsImportInfo(manager, newImportInfos);
                    }

                    // 更新函数的 CapturedScope，确保它们包含模块中定义的所有变量
                    // 这是因为函数定义（FuncInit）在 BlockStatement 中被提升到 ImportStatements，
                    // 导致函数定义时捕获的作用域不包含后来定义的变量
                    UpdateFunctionCapturedScopes(newImportInfos, manager);

                    // 提取符号 - 使用限定范围的 ImportInfos
                    var moduleName = Path.GetFileNameWithoutExtension(modulePath);
                    Dictionary<string, LangValueType> symbols;
                    if (options.ImportSpecifiers is not null && options.ImportSpecifiers.Count > 0)
                    {
                        // 命名导入：从 newImportInfos 中提取指定的符号
                        symbols = _symbolExtractor.ExtractSpecificSymbols(manager, options.ImportSpecifiers, moduleName,
                            newImportInfos);
                    }
                    else
                    {
                        // 通配符导入：只从 newImportInfos 中提取符号
                        symbols = ExtractSymbolsFromNewImportInfos(newImportInfos, manager);
                    }

                    result.ExtractedSymbols = symbols;

                    // 注册到父作用域 - 使用别名（如果有）
                    if (options.SymbolAliases is { Count: > 0 })
                    {
                        // 直接注册到父作用域并应用别名
                        var parentScope = manager.Scopes[^2];
                        foreach (var (originalName, value) in symbols)
                        {
                            var name = options.SymbolAliases.GetValueOrDefault(originalName, originalName);
                            parentScope[name] = value;
                        }
                    }
                    else
                    {
                        _symbolRegistry.RegisterSymbolsToParentScope(manager, symbols);
                    }

                    result.IsSuccess = true;
                }
                finally
                {
                    manager.RemoveChildren();
                }
            }
            else if (options.ModuleAlias is not null)
            {
                // 带别名的导入: import "module" as alias
                // 直接在当前作用域执行模块代码，这样函数可以访问模块变量
                loadResult.Block.Run(manager);

                // 提取符号创建模块对象
                var symbols = _symbolExtractor.ExtractSymbols(manager);
                var moduleName = Path.GetFileNameWithoutExtension(modulePath);
                // 传递 manager 作为捕获作用域，确保函数可以访问模块中的变量
                var moduleObj = UnifiedModule.FromSymbols(moduleName, symbols, default, manager);
                _symbolRegistry.RegisterModule(manager, options.ModuleAlias, moduleObj);
                result.ModuleObject = moduleObj;
                result.IsSuccess = true;
            }
            else
            {
                // 普通导入: import "module"
                // 直接在当前作用域执行模块代码，这样函数可以访问模块变量
                loadResult.Block.Run(manager);

                // 提取符号创建模块对象
                var moduleName = Path.GetFileNameWithoutExtension(modulePath);
                var symbols = _symbolExtractor.ExtractSymbols(manager);
                // 传递 manager 作为捕获作用域，确保函数可以访问模块中的变量
                var moduleObj = UnifiedModule.FromSymbols(moduleName, symbols, default, manager);
                _symbolRegistry.RegisterModule(manager, moduleName, moduleObj);
                result.ModuleObject = moduleObj;
                result.IsSuccess = true;
            }
        }
        finally
        {
            manager.ImportStack.Pop();
        }

        return result;
    }

    /// <summary>
    /// 获取解析器
    /// </summary>
    public ModuleResolver Resolver => _resolver;

    /// <summary>
    /// 获取加载器
    /// </summary>
    public ModuleLoader Loader => _loader;

    /// <summary>
    /// 获取符号提取器
    /// </summary>
    public SymbolExtractor SymbolExtractor => _symbolExtractor;

    /// <summary>
    /// 获取符号注册器
    /// </summary>
    public SymbolRegistry SymbolRegistry => _symbolRegistry;

    /// <summary>
    /// 更新函数的 CapturedScope，确保它们包含模块中定义的所有变量
    /// </summary>
    /// <param name="importInfos">新增的 ImportInfo 列表</param>
    /// <param name="manager">当前的变量管理器（包含模块执行后的完整作用域）</param>
    private static void UpdateFunctionCapturedScopes(List<ImportInfo> importInfos, VariateManager manager)
    {
        // 创建一个映射，用于跟踪需要更新的函数
        var updatedFunctions = new Dictionary<string, ImportInfo>();

        // 遍历所有新增的 ImportInfo
        for (int i = 0; i < importInfos.Count; i++)
        {
            var importInfo = importInfos[i];

            // 只处理 FuncLangValue
            if (importInfo is FuncLangValue func && func.GetCapturedScope() is not null)
            {
                // 创建新的函数副本，使用当前的完整作用域
                var updatedFunc = new FuncLangValue(
                    func.Id,
                    func.Ids ?? [],
                    func.BlockStatement,
                    func.GenericParameters,
                    func.Position,
                    func.Id is null) // IsLambda
                {
                    CapturedScope = manager.Clone(),
                    DocComment = func.DocComment,
                    Decorators = func.Decorators,
                    TypeArgumentMapping = func.TypeArgumentMapping
                };

                // 替换列表中的函数
                importInfos[i] = updatedFunc;

                // 记录需要更新的函数
                if (func.Id?.IdName is not null)
                {
                    updatedFunctions[func.Id.IdName] = updatedFunc;
                }
            }
            // 同样处理 AsyncFuncLangValue
            else if (importInfo is AsyncFuncLangValue asyncFunc && asyncFunc.GetCapturedScope() is not null)
            {
                // 创建新的异步函数副本，使用当前的完整作用域
                var updatedAsyncFunc = new AsyncFuncLangValue(
                    asyncFunc.Id,
                    asyncFunc.Ids ?? [],
                    asyncFunc.BlockStatement,
                    asyncFunc.Position)
                {
                    CapturedScope = manager.Clone(),
                    DocComment = asyncFunc.DocComment,
                    Decorators = asyncFunc.Decorators
                };

                // 替换列表中的函数
                importInfos[i] = updatedAsyncFunc;

                // 记录需要更新的函数
                if (asyncFunc.Id?.IdName is not null)
                {
                    updatedFunctions[asyncFunc.Id.IdName] = updatedAsyncFunc;
                }
            }
        }

        // 更新 manager.ImportInfos 中对应的函数
        if (updatedFunctions.Count > 0)
        {
            var importInfosList = manager.ImportInfos.ToList();
            bool hasChanges = false;

            for (int i = 0; i < importInfosList.Count; i++)
            {
                var info = importInfosList[i];
                string? funcName = info switch
                {
                    FuncLangValue f => f.Id?.IdName,
                    AsyncFuncLangValue af => af.Id?.IdName,
                    _ => null
                };

                if (funcName is not null && updatedFunctions.TryGetValue(funcName, out var updatedFunc))
                {
                    importInfosList[i] = updatedFunc;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                manager.ClearImportInfos();
                manager.AddImportInfoRange(importInfosList);
            }
        }
    }

    /// <summary>
    /// 从新增的 ImportInfo 列表中提取符号（用于通配符导入）
    /// </summary>
    /// <param name="newImportInfos">新增的 ImportInfo 列表</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>符号字典</returns>
    private static Dictionary<string, LangValueType> ExtractSymbolsFromNewImportInfos(
        List<ImportInfo> newImportInfos,
        VariateManager manager)
    {
        var symbols = new Dictionary<string, LangValueType>();

        // 1. 从当前作用域（模块作用域）中提取变量和常量
        if (manager.Scopes.Count > 0)
        {
            var currentScope = manager.Scopes[^1];
            foreach (var (name, value) in currentScope)
            {
                // 跳过模块对象
                if (value is AST.Expression.ModuleObjects.IModuleObject)
                {
                    continue;
                }

                symbols[name] = value;
            }
        }

        // 2. 从 newImportInfos 中提取函数和类
        foreach (var importInfo in newImportInfos)
        {
            string? symbolName = importInfo switch
            {
                FuncLangValue { Id: not null } func => func.Id.IdName,
                AsyncFuncLangValue { Id: not null } asyncFunc => asyncFunc.Id.IdName,
                TypeTemplate template => template.ClassName,
                NativeAnyLangValue nativeAny => nativeAny.RegisterName,
                NativeStaticAny staticAny => staticAny.ClassName,
                ConstantLangValue constant => constant.Name,
                AST.Expression.ModuleObjects.UnifiedModule module => module.ModuleName,
                _ => null
            };

            if (symbolName is not null)
            {
                symbols[symbolName] = importInfo;
            }
        }

        return symbols;
    }
}

/// <summary>
/// 导入选项
/// </summary>
[Serializable]
public class ImportOptions
{
    /// <summary>
    /// 是否使用 from 子句
    /// </summary>
    public bool IsFromClause { get; set; }

    /// <summary>
    /// 模块别名
    /// </summary>
    public string? ModuleAlias { get; set; }

    /// <summary>
    /// 导入指定符（命名导入）- 原始名称列表
    /// </summary>
    public List<string>? ImportSpecifiers { get; set; }

    /// <summary>
    /// 符号别名映射（原始名称 -> 别名）
    /// </summary>
    public Dictionary<string, string>? SymbolAliases { get; set; }

    /// <summary>
    /// 是否懒���载
    /// </summary>
    public bool IsLazy { get; set; }

    /// <summary>
    /// 是否选择性导入
    /// </summary>
    public bool IsSelective { get; set; }
}

/// <summary>
/// 模块导入结果
/// </summary>
[Serializable]
public class ModuleImportResult
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// 解析结果
    /// </summary>
    public ModuleResolutionResult? ResolutionResult { get; set; }

    /// <summary>
    /// 加载的代码块
    /// </summary>
    public BlockStatement? Block { get; set; }

    /// <summary>
    /// 是否来自缓存
    /// </summary>
    public bool IsFromCache { get; set; }

    /// <summary>
    /// 提取的符号
    /// </summary>
    public Dictionary<string, LangValueType>? ExtractedSymbols { get; set; }

    /// <summary>
    /// 模块对象
    /// </summary>
    public LangValueType? ModuleObject { get; set; }
}