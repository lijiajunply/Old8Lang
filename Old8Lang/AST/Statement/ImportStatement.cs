using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.StandardLibrary;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Resolution;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 导入项类，用于表示导入语句中的单个导入项
/// </summary>
/// <param name="name">导入项的原始名称</param>
/// <param name="alias">导入项的别名，默认与原始名称相同</param>
public class ImportItem(string name, string? alias = null)
{
    /// <summary>
    /// 导入项的原始名称
    /// </summary>
    public readonly string Name = name;

    /// <summary>
    /// 导入项的别名，默认与原始名称相同
    /// </summary>
    public readonly string Alias = alias ?? name;
}

/// <summary>
/// 导入语句类，用于处理Old8Lang中的import语句
/// </summary>
/// <param name="importString">导入的模块名称或路径</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <param name="importSpecifiers">导入指定符列表，用于命名导入</param>
/// <param name="fromClause">是否使用from子句，如import { ... } from "module"</param>
/// <param name="moduleAlias">模块别名，如import "module" as alias</param>
/// <param name="isLazy">是否为懒导入，只在首次使用时加载模块</param>
/// <param name="isSelective">是否为选择导入，如 from module import a, b, c</param>
/// <param name="isDynamic">是否为动态导入，模块名在运行时计算</param>
/// <param name="dynamicModuleExpression">动态模块表达式</param>
public partial class ImportStatement(
    string importString,
    SourcePosition position = default,
    List<ImportItem>? importSpecifiers = null,
    bool fromClause = false,
    string? moduleAlias = null,
    bool isLazy = false,
    bool isSelective = false,
    bool isDynamic = false,
    LangExpression? dynamicModuleExpression = null
) : OldStatement(position)
{
    // 静态模块系统服务实例
    private static readonly ModuleSystemService ModuleService = new();

    /// <summary>
    /// 导入的模块名称或路径
    /// </summary>
    private readonly string _importString = importString;

    /// <summary>
    /// 是否使用from子句
    /// </summary>
    private readonly bool _fromClause = fromClause;

    /// <summary>
    /// 导入指定符列表，用于命名导入
    /// </summary>
    private readonly List<ImportItem> _importSpecifiers = importSpecifiers ?? [];

    /// <summary>
    /// 模块别名，如import "module" as alias
    /// </summary>
    private readonly string? _moduleAlias = moduleAlias;

    /// <summary>
    /// 是否为懒导入，只在首次使用时加载模块
    /// </summary>
    private readonly bool _isLazy = isLazy;

    /// <summary>
    /// 是否为选择导入，如 from module import a, b, c
    /// </summary>
    private readonly bool _isSelective = isSelective;

    /// <summary>
    /// 是否为动态导入，模块名在运行时计算
    /// </summary>
    private readonly bool _isDynamic = isDynamic;

    /// <summary>
    /// 公共属性：是否为动态导入
    /// </summary>
    public bool IsDynamicImport => _isDynamic;

    /// <summary>
    /// 公共属性：导入的模块名称或路径
    /// </summary>
    public string GetImportString() => _importString;

    /// <summary>
    /// 公共属性：是否使用from子句
    /// </summary>
    public bool GetFromClause() => _fromClause;

    /// <summary>
    /// 公共属性：导入指定符列表
    /// </summary>
    public List<ImportItem> GetImportSpecifiers() => _importSpecifiers;

    /// <summary>
    /// 公共属性：模块别名
    /// </summary>
    public string? GetModuleAlias() => _moduleAlias;

    /// <summary>
    /// 动态模块表达式，用于在运行时计算模块名
    /// </summary>
    private readonly LangExpression? _dynamicModuleExpression = dynamicModuleExpression;

    /// <summary>
    /// 在解释模式下执行导入语句
    /// </summary>
    /// <param name="manager">变量管理器，用于管理导入的模块和变量</param>
    /// <exception cref="ImportError">当导入失败时抛出</exception>
    public override void Run(VariateManager manager)
    {
        var moduleName = _importString;

        // 动态导入处理
        if (_isDynamic)
        {
            HandleDynamicImport(manager);
            return;
        }

        // 懒导入处理
        if (_isLazy)
        {
            HandleLazyImport(manager);
            return;
        }

        var symbolAliases = _importSpecifiers
            .Where(item => item.Name != item.Alias)
            .ToDictionary(item => item.Name, item => item.Alias);

        var options = new ImportOptions
        {
            IsFromClause = _fromClause,
            ModuleAlias = _moduleAlias,
            ImportSpecifiers = _importSpecifiers.Select(item => item.Name).ToList(),
            SymbolAliases = symbolAliases.Count > 0 ? symbolAliases : null,
            IsLazy = _isLazy,
            IsSelective = _isSelective
        };

        var result = ModuleService.ImportModule(moduleName, manager, options);

        if (!result.IsSuccess)
        {
            if (result.Error is not null)
            {
                throw result.Error;
            }

            // 使用 ModuleResolutionResult 提供详细错误信息
            if (result.ResolutionResult is not null)
            {
                throw new ImportError(this, moduleName, result.ResolutionResult);
            }

            throw new ImportError(this, moduleName, "模块导入失败");
        }
    }

    /// <summary>
    /// 在编译模式下生成导入语句的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器，用于管理导入的模块和变量</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        string moduleName = _importString;
        bool isDirectory = false;

        // 优先级 1: 标准库（Old8LangLib 和 Old8Lang.NetLib）
        // 在编译模式下，标准库由 C# 运行时提供，但我们需要验证其是否可用
        if (StandardLibraryRegistry.IsStandardLibrary(moduleName))
        {
            // 验证标准库是否可用（程序集和类是否存在）
            if (!StandardLibraryLoader.ValidateStandardLibrary(moduleName, out var errorMessage))
            {
                throw new ImportError(Position, moduleName,
                    $"编译时验证失败: {errorMessage}\n\n" +
                    "请确保标准库程序集已正确部署到运行时目录。");
            }

            // 标准库在编译模式下不需要生成 IL（运行时会处理）
            // 但已经过验证，确保编译后能正常运行
            return;
        }

        // 使用 ModuleResolver 进行统一的模块解析
        var resolver = new ModuleResolver();
        var resolution = resolver.ResolveModule(moduleName, local.FilePath);

        // 如果解析失败，抛出详细错误
        if (!resolution.IsSuccess)
        {
            throw new ImportError(Position, moduleName, resolution);
        }

        // 处理不同类型的模块
        string? resolvedPath = resolution.ResolvedPath;

        // 如果是网络模块，编译模式暂不支持
        if (resolution.ModuleType == ModuleType.NetworkModule)
        {
            throw new ImportError(Position, moduleName,
                "编译模式暂不支持从网络URL导入模块。请使用解释模式 (-f) 或将模块下载到本地。");
        }

        // 对于本地文件、第三方包和子模块，继续处理
        if (string.IsNullOrEmpty(resolvedPath))
        {
            // 这种情况不应该发生，因为 IsSuccess 为 false 时已经抛出异常
            throw new ImportError(Position, moduleName, "模块解析失败，但未提供详细信息");
        }

        // 获取绝对路径作为缓存键
        var moduleAbsolutePath = Path.GetFullPath(resolvedPath);

        // 检查模块缓存
        if (local.Interpreter?.ModuleCache.TryGetValue(moduleAbsolutePath, out var cachedBlock) == true)
        {
            // 使用缓存的模块
            var cachedOriginalPath = local.FilePath;
            local.FilePath = moduleAbsolutePath;
            cachedBlock.GenerateImportIl(ilGenerator, local);
            local.FilePath = cachedOriginalPath;
            return;
        }

        // 执行导入并缓存
        var code = isDirectory ? Apis.FromDirectory(moduleAbsolutePath) : Apis.FromFile(moduleAbsolutePath);
        var importOriginalPath = local.FilePath;
        local.FilePath = moduleAbsolutePath;
        var block = local.Interpreter?.Build(code: code);

        // 缓存模块
        if (block is not null && local.Interpreter is not null)
        {
            local.Interpreter.ModuleCache[moduleAbsolutePath] = block;
        }

        block?.GenerateImportIl(ilGenerator, local);
        local.FilePath = importOriginalPath;
    }

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为ImportStatement是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为ImportStatement是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 处理懒导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void HandleLazyImport(VariateManager manager)
    {
        // 检查是否启用新的统一工厂
        // 使用新的统一模块工厂创建模块对象
        UnifiedModule moduleValue;

        if (_isSelective && _importSpecifiers.Count > 0)
        {
            // 选择性导入
            var selectedSymbols = _importSpecifiers.Select(item => item.Alias).ToList();
            moduleValue = ModuleFactory.CreateSelectiveModule(_importString, selectedSymbols, manager, Position);
        }
        else
        {
            // 懒加载模块
            moduleValue = ModuleFactory.CreateLazyModule(_importString, manager, Position);
        }

        // 注册模块对象到变量管理器
        RegisterModuleValue(moduleValue, manager);
    }

    /// <summary>
    /// 注册模块值对象到变量管理器
    /// </summary>
    /// <param name="moduleValue">模块值对象</param>
    /// <param name="manager">变量管理器</param>
    private void RegisterModuleValue(UnifiedModule moduleValue, VariateManager manager)
    {
        ArgumentNullException.ThrowIfNull(moduleValue);
        if (_moduleAlias is not null)
        {
            manager.Scopes[^1][_moduleAlias] = moduleValue; // 带别名的导入：使用别名注册
        }
        else if (_isSelective && _importSpecifiers.Count > 0)
        {
            // 选择性懒导入：创建符号代理，延迟加载到实际访问时
            if (_isLazy)
            {
                // 懒加载选择性导入：创建代理符号
                foreach (var specifier in _importSpecifiers)
                {
                    var symbolName = specifier.Alias;
                    var originalName = specifier.Name;

                    // 创建懒加载代理：使用 LazySymbolProxy
                    var proxy = new LazySymbolProxy(moduleValue, originalName, Position);
                    manager.Scopes[^1][symbolName] = proxy;
                }
            }
            else
            {
                // 即时加载选择性导入：直接获取符号
                foreach (var specifier in _importSpecifiers)
                {
                    var symbolName = specifier.Alias;
                    var symbol = moduleValue.GetSymbol(specifier.Name);
                    if (symbol is not null)
                    {
                        manager.Scopes[^1][symbolName] = symbol;
                    }
                    else
                    {
                        throw new ImportError(this, specifier.Name,
                            $"Symbol '{specifier.Name}' not found in module '{moduleValue.ModuleName}'");
                    }
                }
            }
        }
        else if (!_fromClause && _moduleAlias is null && _isLazy)
        {
            // 通配符懒导入：lazy import "module" (无别名、无 from 子句)
            // 将所有符号以代理形式注册到当前作用域
            // 由于无法在不加载模块的情况下知道有哪些符号，我们采用延迟策略：
            // 注册一个特殊的标记，当符号查找失败时，尝试从懒加载模块中查找
            manager.AddLazyWildcardModule(moduleValue);
        }
        else
        {
            // 普通导入：使用模块名注册
            var moduleName = Path.GetFileNameWithoutExtension(_importString.Trim('"'));
            manager.Scopes[^1][moduleName] = moduleValue;
        }
    }

    /// <summary>
    /// 将导入语句转换为字符串表示
    /// </summary>
    /// <returns>导入语句的字符串表示</returns>
    public override string ToString()
    {
        var lazyStr = _isLazy ? "lazy " : "";
        var dynamicStr = _isDynamic ? "dynamic " : "";

        if (_isSelective)
        {
            var specifiers = string.Join(", ",
                _importSpecifiers.Select(s => s.Name == s.Alias ? s.Name : $"{s.Name} as {s.Alias}"));
            var prefix = _isDynamic ? $"{dynamicStr}import {specifiers} from " : $"{lazyStr}import {specifiers} from ";
            return $"{prefix}{(_isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString)}";
        }

        if (_importSpecifiers.Count > 0)
        {
            var specifiers = string.Join(", ",
                _importSpecifiers.Select(s => s.Name == s.Alias ? s.Name : $"{s.Name} as {s.Alias}"));
            var prefix = _isDynamic
                ? $"{dynamicStr}import {{ {specifiers} }} from "
                : $"{lazyStr}import {{ {specifiers} }} from ";
            return $"{prefix}{(_isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString)}";
        }

        if (_moduleAlias is not null)
        {
            var prefix = _isDynamic ? $"{dynamicStr}import " : $"{lazyStr}import ";
            var modulePart = _isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString;
            return $"{prefix}{modulePart} as {_moduleAlias}";
        }

        var basePrefix = _isDynamic ? $"{dynamicStr}import " : $"{lazyStr}import ";
        var baseModule = _isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString;
        return $"{basePrefix}{baseModule}";
    }

    /// <summary>
    /// 处理动态导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <exception cref="ImportError">当动态导入失败时抛出</exception>
    private void HandleDynamicImport(VariateManager manager)
    {
        if (_dynamicModuleExpression is null)
        {
            throw new ImportError(this, _importString, "Dynamic import expression is null");
        }

        try
        {
            // 运行动态模块表达式来获取模块名
            var dynamicResult = _dynamicModuleExpression.Run(manager);

            if (dynamicResult is StringLangValue stringModuleValue)
            {
                var actualModuleName = stringModuleValue.Value;

                // 使用新的统一模块工厂创建模块对象
                UnifiedModule unifiedModule;

                if (_isSelective && _importSpecifiers.Count > 0)
                {
                    // 选择性导入
                    var selectedSymbols = _importSpecifiers.Select(item => item.Alias).ToList();
                    unifiedModule =
                        ModuleFactory.CreateSelectiveModule(actualModuleName, selectedSymbols, manager, Position);
                }
                else
                {
                    // 动态导入创建即时加载模块（因为已经确定要导入）
                    unifiedModule = ModuleFactory.CreateEagerModule(actualModuleName, manager, Position);
                }

                // 注册模块对象到变量管理器
                RegisterModuleValue(unifiedModule, manager);
            }
            else
            {
                throw new ImportError(this, _importString,
                    $"Dynamic import expression must evaluate to a string, got {dynamicResult.GetType().Name}");
            }
        }
        catch (ImportError)
        {
            // 重新抛出导入错误
            throw;
        }
        catch (Exception ex)
        {
            // 包装其他异常为导入错误
            throw new ImportError(this, _importString, $"Dynamic import failed: {ex.Message}");
        }
    }
}