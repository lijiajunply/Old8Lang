using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.StandardLibrary;

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
    /// <summary>
    /// 导入的模块名称或路径
    /// </summary>
    private readonly string ImportString = importString;

    /// <summary>
    /// 是否使用from子句
    /// </summary>
    private readonly bool FromClause = fromClause;

    /// <summary>
    /// 导入指定符列表，用于命名导入
    /// </summary>
    private readonly List<ImportItem> ImportSpecifiers = importSpecifiers ?? [];

    /// <summary>
    /// 模块别名，如import "module" as alias
    /// </summary>
    private readonly string? ModuleAlias = moduleAlias;

    /// <summary>
    /// 是否为懒导入，只在首次使用时加载模块
    /// </summary>
    private readonly bool IsLazy = isLazy;

    /// <summary>
    /// 是否为选择导入，如 from module import a, b, c
    /// </summary>
    private readonly bool IsSelective = isSelective;

    /// <summary>
    /// 是否为动态导入，模块名在运行时计算
    /// </summary>
    private readonly bool IsDynamic = isDynamic;

    /// <summary>
    /// 公共属性：是否为动态导入
    /// </summary>
    public bool IsDynamicImport => IsDynamic;

    /// <summary>
    /// 动态模块表达式，用于在运行时计算模块名
    /// </summary>
    private readonly LangExpression? DynamicModuleExpression = dynamicModuleExpression;

    /// <summary>
    /// 在解释模式下执行导入语句
    /// </summary>
    /// <param name="manager">变量管理器，用于管理导入的模块和变量</param>
    /// <exception cref="ImportError">当导入失败时抛出</exception>
    public override void Run(VariateManager manager)
    {
        var moduleName = ImportString;
        var attemptedPaths = new List<string>();
        string? resolvedPath;
        bool isDirectory = false;

        // 检查是否为网络路径（URL）
        if (moduleName.StartsWith("http://") || moduleName.StartsWith("https://"))
        {
            // 网络导入警告
            manager.Interpreter.OutputProvider.WriteLine($"[警告] 正在从网络导入模块: {moduleName}");
            manager.Interpreter.OutputProvider.WriteLine("[警告] 网络导入存在安全风险，请确保来源可信");

            // 网络路径特殊处理
            // 由于我们还不支持真正的网络导入，我们可以创建一个简单的模块对象
            // 直接进入执行阶段，跳过文件系统检查
            if (ModuleAlias != null)
            {
                // 创建统一模块对象
                var moduleObj = ModuleFactory.CreateEagerModule(ImportString, manager, Position);

                // 将模块对象添加到当前作用域
                manager.Scopes[^1][ModuleAlias] = moduleObj;
            }

            return;
        }

        // 处理 module.submodule 语法
        if (moduleName.Contains('.'))
        {
            HandleSubmoduleImport(moduleName, manager);
            return;
        }

        // 动态导入处理
        if (IsDynamic)
        {
            HandleDynamicImport(manager);
            return;
        }

        // 懒导入处理
        if (IsLazy)
        {
            HandleLazyImport(manager);
            return;
        }

        // 优先级 1: 标准库（Old8LangLib 和 Old8Lang.NetLib）
        if (StandardLibraryRegistry.IsStandardLibrary(moduleName))
        {
            if (StandardLibraryLoader.TryLoadStandardLibrary(moduleName, manager, out var stdModule))
            {
                RegisterModule(manager, moduleName, stdModule);
                return;
            }
        }

        // 优先级 2: 第三方包（通过 PackageManager）
        var packageManager = manager.GetPackageManager();

        // 根据当前执行文件添加包查找路径
        packageManager.AddSearchPathsFromSourceFile(manager.Path);

        if (packageManager.TryLoadPackage(moduleName, manager, out var pkgModule))
        {
            RegisterModule(manager, moduleName, pkgModule);
            return;
        }

        // 优先级 3: 本地文件导入（相对于当前文件的路径）

        var dic = Path.GetDirectoryName(manager.Path);
        // 检查文件扩展名，只支持.old8和.ol
        var fileNameLocal = moduleName;
        var extLocal = Path.GetExtension(fileNameLocal).ToLower();
        if (extLocal != ".old8" && extLocal != ".ol")
        {
            fileNameLocal += ".old8"; // 默认使用.old8扩展名
        }

        // 修复：正确处理绝对路径和相对路径
        var filePath = Path.IsPathRooted(fileNameLocal)
            ? fileNameLocal
            : dic != null
                ? Path.Combine(dic, fileNameLocal)
                : fileNameLocal;

        if (filePath.StartsWith("Users/") || filePath.StartsWith("Volumes/"))
        {
            filePath = "/" + filePath;
        }

        attemptedPaths.Add(filePath);

        if (!File.Exists(filePath))
        {
            throw new ImportError(Position, moduleName, attemptedPaths);
        }

        resolvedPath = filePath;


        if (resolvedPath == null)
        {
            throw new ImportError(Position, moduleName, attemptedPaths);
        }

        // 获取绝对路径作为缓存键
        var moduleAbsolutePath = Path.GetFullPath(resolvedPath);

        // 1. 检查循环依赖
        if (manager.ImportStack.Contains(moduleAbsolutePath))
        {
            throw new ImportError(Position, moduleName, manager.ImportStack);
        }

        // 2. 检查模块缓存
        if (manager.Interpreter.ModuleCache.TryGetValue(moduleAbsolutePath, out var cachedBlock))
        {
            // 使用缓存的模块
            if (!FromClause) return;
            // 为命名导入创建独立作用域
            manager.AddChildren();
            // 对于缓存的模块，我们不需要再次执行它的函数和类定义语句
            // 我们只需要执行它的变量赋值语句
            // 函数和类已经在全局作用域中了
            cachedBlock.ExecuteModule(manager, skipFunctionClassInit: true);
            // 只导入指定的成员
            ImportSpecifiedMembers(manager);
            manager.RemoveChildren();

            return;
        }

        // 3. 执行导入
        manager.ImportStack.Push(moduleAbsolutePath);
        try
        {
            var previousPath = manager.Path;
            manager.Path = moduleAbsolutePath;

            var code = isDirectory ? Apis.FromDirectory(moduleAbsolutePath) : Apis.FromFile(moduleAbsolutePath);
            var block = manager.Interpreter.Build(code: code);

            // 4. 缓存模块
            manager.Interpreter.ModuleCache[moduleAbsolutePath] = block;

            if (FromClause)
            {
                // 为命名导入创建独立作用域
                manager.AddChildren();
                // 执行模块的非导入语句，包括函数定义、类定义和变量赋值
                // 但跳过导入语句，避免递归导入
                block.ExecuteModule(manager);
                // 只导入指定的成员
                ImportSpecifiedMembers(manager);
                manager.RemoveChildren();
            }
            else if (ModuleAlias != null)
            {
                // 对于带别名的导入，创建新的统一模块对象
                var moduleObj = ModuleFactory.CreateEagerModule(ImportString, manager, Position);

                // 先执行模块代码来填充符号
                block.Run(manager);

                // 将模块对象添加到当前作用域
                manager.Scopes[^1][ModuleAlias] = moduleObj;
            }
            else
            {
                // 使用新的统一模块工厂创建模块对象
                UnifiedModule moduleValue;

                if (IsSelective && ImportSpecifiers.Count > 0)
                {
                    // 选择性导入
                    var selectedSymbols = ImportSpecifiers.Select(item => item.Alias).ToList();
                    moduleValue =
                        ModuleFactory.CreateSelectiveModule(ImportString, selectedSymbols, manager, Position);
                }
                else
                {
                    // 懒加载模块
                    moduleValue = ModuleFactory.CreateLazyModule(ImportString, manager, Position);
                }

                // 先执行模块代码来填充符号（如果不是选择性导入）
                if (!IsSelective)
                {
                    block.Run(manager);
                }

                // 注册模块对象到变量管理器
                RegisterModuleValue(moduleValue, manager);
            }

            manager.Path = previousPath;
        }
        finally
        {
            // 确保无论导入成功与否，都从导入栈中移除
            manager.ImportStack.Pop();
        }
    }

    /// <summary>
    /// 只导入指定的成员到当前作用域
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void ImportSpecifiedMembers(VariateManager manager)
    {
        // 获取当前作用域的所有变量（模块导出的成员）
        var currentScope = manager.Scopes[^1];
        var parentScope = manager.Scopes[^2];

        // 如果没有指定导入成员，则导入所有成员
        if (ImportSpecifiers.Count == 0)
        {
            // 直接将所有成员添加到父作用域
            foreach (var (name, value) in currentScope)
            {
                parentScope[name] = value;
            }

            return;
        }

        // 只导入指定的成员
        foreach (var specifier in ImportSpecifiers)
        {
            // 首先尝试从当前作用域中查找（变量和常量）
            if (currentScope.TryGetValue(specifier.Name, out var value))
            {
                // 将指定成员添加到父作用域，支持重命名
                parentScope[specifier.Alias] = value;
            }
            // 然后尝试从父作用域中查找（可能是之前导入的变量）
            else if (parentScope.TryGetValue(specifier.Name, out value))
            {
                // 将指定成员添加到父作用域，支持重命名
                parentScope[specifier.Alias] = value;
            }
            // 如果仍然找不到，抛出错误
            else
            {
                // 成员不存在，抛出错误
                throw new ImportError(Position, ImportString, [ImportString]);
            }
        }
    }

    /// <summary>
    /// 注册模块到当前作用域
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="module">模块对象</param>
    private void RegisterModule(VariateManager manager, string moduleName, LangValueType? module)
    {
        if (module == null)
            return;

        if (FromClause)
        {
            // 命名导入：导入模块中的指定成员
            if (module is IModuleValueType moduleValue)
            {
                manager.AddChildren();
                var symbolNames = moduleValue.GetExportedSymbols();

                // 将符号添加到当前作用域
                foreach (var symbolName in symbolNames)
                {
                    var symbolValue = moduleValue.GetSymbol(symbolName);
                    if (symbolValue != null)
                    {
                        manager.Scopes[^1][symbolName] = symbolValue;
                    }
                }

                // 导入指定的成员到父作用域
                ImportSpecifiedMembers(manager);
                manager.RemoveChildren();
            }
        }
        else if (ModuleAlias != null)
        {
            // 带别名的导入：import Module as Alias
            manager.Scopes[^1][ModuleAlias] = module;
        }
        else
        {
            // 默认导入：import Module
            manager.Scopes[^1][moduleName] = module;
        }
    }

    /// <summary>
    /// 在编译模式下生成导入语句的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器，用于管理导入的模块和变量</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        string moduleName = ImportString;
        string? resolvedPath;
        bool isDirectory = false;

        var langInfo = Apis.ReadJson();
        if (langInfo.LibInfos.Any(x => moduleName == x.LibName))
        {
            var libInfo = langInfo.LibInfos.First(x => x.LibName == moduleName);
            isDirectory = libInfo.IsDir;

            // 检查文件扩展名，只支持.old8和.ol
            var fileName = moduleName;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!isDirectory && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            resolvedPath = Path.Combine(langInfo.ImportPath, fileName);
        }
        else
        {
            var dic = Path.GetDirectoryName(local.FilePath)!;
            // 检查文件扩展名，只支持.old8和.ol
            var fileNameLocal = moduleName;
            var extLocal = Path.GetExtension(fileNameLocal).ToLower();
            if (extLocal != ".old8" && extLocal != ".ol")
            {
                fileNameLocal += ".old8"; // 默认使用.old8扩展名
            }

            // 修复：正确处理绝对路径和相对路径
            resolvedPath = Path.IsPathRooted(fileNameLocal) ? fileNameLocal : Path.Combine(dic, fileNameLocal);
            if (resolvedPath.StartsWith("Users/") || resolvedPath.StartsWith("Volumes/"))
            {
                resolvedPath = "/" + resolvedPath;
            }

            if (!File.Exists(resolvedPath)) return;
        }

        if (string.IsNullOrEmpty(resolvedPath))
        {
            return;
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
        if (block != null && local.Interpreter != null)
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

        if (IsSelective && ImportSpecifiers.Count > 0)
        {
            // 选择性导入
            var selectedSymbols = ImportSpecifiers.Select(item => item.Alias).ToList();
            moduleValue = ModuleFactory.CreateSelectiveModule(ImportString, selectedSymbols, manager, Position);
        }
        else
        {
            // 懒加载模块
            moduleValue = ModuleFactory.CreateLazyModule(ImportString, manager, Position);
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
        if (ModuleAlias != null)
        {
            manager.Scopes[^1][ModuleAlias] = moduleValue; // 带别名的导入：使用别名注册
        }
        else if (IsSelective && ImportSpecifiers.Count > 0)
        {
            // 选择性导入：将每个符号直接注册到作用域
            foreach (var specifier in ImportSpecifiers)
            {
                var symbolName = specifier.Alias;
                var symbol = moduleValue.GetSymbol(specifier.Name);
                if (symbol != null)
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
        else
        {
            // 普通导入：使用模块名注册
            var moduleName = Path.GetFileNameWithoutExtension(ImportString.Trim('"'));
            manager.Scopes[^1][moduleName] = moduleValue;
        }
    }

    /// <summary>
    /// 处理子模块导入 module.submodule
    /// </summary>
    /// <param name="moduleName">模块名称，如 "package.submodule"</param>
    /// <param name="manager">变量管理器</param>
    private void HandleSubmoduleImport(string moduleName, VariateManager manager)
    {
        var parts = moduleName.Split('.');
        var basePath = manager.LangInfo!.ImportPath;
        var currentPath = basePath;

        // 逐级查找子模块
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var testPath = Path.Combine(currentPath, part);

            if (i == parts.Length - 1)
            {
                // 最后一个部分，查找 .old8 文件或目录
                var filePath = testPath + ".old8";
                var dirPath = testPath;

                if (File.Exists(filePath))
                {
                    // 找到文件，执行导入
                    ImportModuleFile(filePath, manager, parts[i], ModuleAlias);
                    return;
                }

                if (Directory.Exists(dirPath))
                {
                    // 找到目录，查找 __init__.old8 或 index.old8
                    var initFile = Path.Combine(dirPath, "__init__.old8");
                    var indexFile = Path.Combine(dirPath, "index.old8");

                    if (File.Exists(initFile))
                    {
                        ImportModuleFile(initFile, manager, parts[i], ModuleAlias);
                        return;
                    }

                    if (File.Exists(indexFile))
                    {
                        ImportModuleFile(indexFile, manager, parts[i], ModuleAlias);
                        return;
                    }
                }
            }
            else if (Directory.Exists(testPath))
            {
                // 中间路径，继续深入
                currentPath = testPath;
            }
            else
            {
                throw new ImportError(Position, moduleName, [testPath]);
            }
        }
    }

    /// <summary>
    /// 导入模块文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="moduleName">模块名</param>
    /// <param name="alias">别名</param>
    private void ImportModuleFile(string filePath, VariateManager manager, string moduleName, string? alias)
    {
        var moduleAbsolutePath = Path.GetFullPath(filePath);

        // 检查循环依赖
        if (manager.ImportStack.Contains(moduleAbsolutePath))
        {
            throw new ImportError(Position, moduleName, manager.ImportStack);
        }

        // 检查缓存
        if (manager.Interpreter.ModuleCache.TryGetValue(moduleAbsolutePath, out var cachedBlock))
        {
            // 使用缓存的模块
            if (FromClause)
            {
                manager.AddChildren();
                cachedBlock.ExecuteModule(manager, skipFunctionClassInit: true);
                ImportSpecifiedMembers(manager);
                manager.RemoveChildren();
            }
            else
            {
                if (alias != null)
                {
                    var moduleObj = ModuleFactory.CreateEagerModule(moduleName, manager, Position);
                    manager.Scopes[^1][alias] = moduleObj;
                }
            }

            return;
        }

        // 执行导入
        manager.ImportStack.Push(moduleAbsolutePath);
        try
        {
            var previousPath = manager.Path;
            manager.Path = moduleAbsolutePath;

            var code = Apis.FromFile(moduleAbsolutePath);
            var block = manager.Interpreter.Build(code: code);

            // 缓存模块
            manager.Interpreter.ModuleCache[moduleAbsolutePath] = block;

            if (FromClause)
            {
                manager.AddChildren();
                block.ExecuteModule(manager);
                ImportSpecifiedMembers(manager);
                manager.RemoveChildren();
            }
            else
            {
                block.Run(manager);

                if (alias != null)
                {
                    var moduleObj = ModuleFactory.CreateEagerModule(moduleName, manager, Position);
                    manager.Scopes[^1][alias] = moduleObj;
                }
            }

            manager.Path = previousPath;
        }
        finally
        {
            manager.ImportStack.Pop();
        }
    }

    /// <summary>
    /// 将导入语句转换为字符串表示
    /// </summary>
    /// <returns>导入语句的字符串表示</returns>
    public override string ToString()
    {
        var lazyStr = IsLazy ? "lazy " : "";
        var dynamicStr = IsDynamic ? "dynamic " : "";

        if (IsSelective)
        {
            var specifiers = string.Join(", ",
                ImportSpecifiers.Select(s => s.Name == s.Alias ? s.Name : $"{s.Name} as {s.Alias}"));
            var prefix = IsDynamic ? $"{dynamicStr}import {specifiers} from " : $"{lazyStr}import {specifiers} from ";
            return $"{prefix}{(IsDynamic ? DynamicModuleExpression?.ToString() ?? ImportString : ImportString)}";
        }

        if (ImportSpecifiers.Count > 0)
        {
            var specifiers = string.Join(", ",
                ImportSpecifiers.Select(s => s.Name == s.Alias ? s.Name : $"{s.Name} as {s.Alias}"));
            var prefix = IsDynamic
                ? $"{dynamicStr}import {{ {specifiers} }} from "
                : $"{lazyStr}import {{ {specifiers} }} from ";
            return $"{prefix}{(IsDynamic ? DynamicModuleExpression?.ToString() ?? ImportString : ImportString)}";
        }

        if (ModuleAlias != null)
        {
            var prefix = IsDynamic ? $"{dynamicStr}import " : $"{lazyStr}import ";
            var modulePart = IsDynamic ? DynamicModuleExpression?.ToString() ?? ImportString : ImportString;
            return $"{prefix}{modulePart} as {ModuleAlias}";
        }

        var basePrefix = IsDynamic ? $"{dynamicStr}import " : $"{lazyStr}import ";
        var baseModule = IsDynamic ? DynamicModuleExpression?.ToString() ?? ImportString : ImportString;
        return $"{basePrefix}{baseModule}";
    }

    /// <summary>
    /// 处理动态导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <exception cref="ImportError">当动态导入失败时抛出</exception>
    private void HandleDynamicImport(VariateManager manager)
    {
        if (DynamicModuleExpression == null)
        {
            throw new ImportError(this, ImportString, "Dynamic import expression is null");
        }

        try
        {
            // 运行动态模块表达式来获取模块名
            var dynamicResult = DynamicModuleExpression.Run(manager);

            if (dynamicResult is StringLangValue stringModuleValue)
            {
                var actualModuleName = stringModuleValue.Value;

                // 使用新的统一模块工厂创建模块对象
                UnifiedModule unifiedModule;

                if (IsSelective && ImportSpecifiers.Count > 0)
                {
                    // 选择性导入
                    var selectedSymbols = ImportSpecifiers.Select(item => item.Alias).ToList();
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
                throw new ImportError(this, ImportString,
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
            throw new ImportError(this, ImportString, $"Dynamic import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查指定的模块是否安装到了
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>是否为本地安装库</returns>
    private static bool IsInLibrary(string moduleName, VariateManager manager)
    {
        // 基于LangInfo.json配置来识别标准库
        if (manager.LangInfo?.LibInfos == null)
            return false;

        return manager.LangInfo.LibInfos.Any(lib =>
            string.Equals(lib.LibName, moduleName, StringComparison.OrdinalIgnoreCase));
    }
}