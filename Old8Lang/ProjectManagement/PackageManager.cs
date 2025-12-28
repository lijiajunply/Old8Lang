using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Interpreter;

namespace Old8Lang.ProjectManagement;

/// <summary>
/// Old8Lang 运行时包加载器
/// 负责：
/// 1. 运行时包加载和执行
/// 2. 包路径解析和缓存管理
///
/// 注意：包安装功能委托给 Old8Lang.PackageManager.Core
/// </summary>
public class PackageManager
{
    /// <summary>
    /// 包缓存（已加载的包）
    /// </summary>
    private readonly Dictionary<string, LangValueType> PackageCache = new();

    /// <summary>
    /// 包查找路径列表
    /// </summary>
    private readonly List<string> PackageSearchPaths = [];

    /// <summary>
    /// 包加载锁
    /// </summary>
    private readonly Lock LoadLock = new();

    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    public static bool DebugEnabled { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="packagesDir">包目录路径，null 则使用默认路径</param>
    /// <param name="projectRoot">项目根目录（保留参数以兼容现有调用，但不再使用）</param>
    public PackageManager(string? packagesDir = null, string? projectRoot = null)
    {
        // 添加全局包目录
        var packagesDirectory = packagesDir ?? GetDefaultPackagesDirectory();
        AddSearchPath(packagesDirectory);

        LogDebug($"PackageManager initialized with {PackageSearchPaths.Count} search paths:");
        foreach (var path in PackageSearchPaths)
        {
            LogDebug($"  - {path} (exists: {Directory.Exists(path)})");
        }
    }

    /// <summary>
    /// 获取默认包目录
    /// </summary>
    private static string GetDefaultPackagesDirectory()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".old8lang", "packages");
    }

    /// <summary>
    /// 添加包查找路径
    /// </summary>
    public void AddSearchPath(string path)
    {
        if (PackageSearchPaths.Contains(path)) return;
        PackageSearchPaths.Add(path);
        LogDebug($"Added search path: {path}");
    }

    /// <summary>
    /// 根据源文件路径添加包查找路径
    /// 只添加源文件所在目录的 packages 子目录
    /// </summary>
    public void AddSearchPathsFromSourceFile(string? sourceFilePath)
    {
        if (string.IsNullOrEmpty(sourceFilePath))
            return;

        try
        {
            var sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));
            if (string.IsNullOrEmpty(sourceDir))
                return;

            // 添加源文件所在目录的 packages 子目录
            var localPackages = Path.Combine(sourceDir, "packages");
            if (Directory.Exists(localPackages))
            {
                AddSearchPath(localPackages);
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Error adding search paths from source file: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试加载第三方包
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="module">输出的模块对象</param>
    /// <returns>是否成功加载</returns>
    public bool TryLoadPackage(
        string packageName,
        VariateManager manager,
        out LangValueType? module)
    {
        lock (LoadLock)
        {
            LogDebug($"Attempting to load package: {packageName}");

            // 检查缓存
            if (PackageCache.TryGetValue(packageName, out module))
            {
                LogDebug($"Package '{packageName}' found in cache");
                return true;
            }

            LogDebug($"Package '{packageName}' not in cache, searching in {PackageSearchPaths.Count} paths:");

            // 在所有查找路径中搜索包
            foreach (var searchPath in PackageSearchPaths)
            {
                // 策略 1: 尝试精确目录名
                var packagePath = Path.Combine(searchPath, packageName);
                LogDebug($"  Checking exact match: {packagePath}");

                if (TryLoadPackageFromPath(packagePath, packageName, manager, out module) && module != null)
                {
                    LogDebug($"  ✓ Package '{packageName}' loaded successfully from: {packagePath}");
                    PackageCache[packageName] = module;
                    return true;
                }

                // 策略 2: 尝试版本化目录（PackageName@*）
                if (!Directory.Exists(searchPath)) continue;
                var versionedDirs = Directory.GetDirectories(searchPath, $"{packageName}@*");
                if (versionedDirs.Length <= 0) continue;
                // 选择第一个匹配的版本（未来可以改进为选择最新版本）
                var versionedPath = versionedDirs[0];
                LogDebug($"  Found versioned directory: {versionedPath}");

                if (!TryLoadPackageFromPath(versionedPath, packageName, manager, out module) ||
                    module == null) continue;
                LogDebug($"  ✓ Package '{packageName}' loaded successfully from: {versionedPath}");
                PackageCache[packageName] = module;
                return true;
            }

            LogDebug($"Package '{packageName}' not found in any search path");
            module = null;
            return false;
        }
    }

    /// <summary>
    /// 从指定路径加载包
    /// </summary>
    private bool TryLoadPackageFromPath(
        string packagePath,
        string packageName,
        VariateManager manager,
        out LangValueType? module)
    {
        if (!Directory.Exists(packagePath))
        {
            LogDebug($"    Directory does not exist: {packagePath}");
            module = null;
            return false;
        }

        LogDebug($"    Found directory: {packagePath}");

        try
        {
            // 查找包的入口文件
            var entryFile = FindPackageEntryFile(packagePath, packageName);

            if (entryFile == null)
            {
                LogDebug($"    No entry file found in: {packagePath}");
                module = null;
                return false;
            }

            LogDebug($"    Entry file: {entryFile}");

            // 直接加载包文件并提取符号
            var previousPath = manager.Path;
            manager.Path = entryFile;

            try
            {
                // 读取并执行包代码
                var code = File.ReadAllText(entryFile);
                var block = manager.Interpreter.Build(code: code);

                // 创建临时作用域来捕获包导出的符号
                var tempScope = new Dictionary<string, LangValueType>();
                manager.Scopes.Add(tempScope);

                try
                {
                    // 执行包代码
                    block.Run(manager);

                    // 提取符号（函数和变量）
                    var symbols = new Dictionary<string, LangValueType>();
                    foreach (var (symbolName, symbolValue) in tempScope)
                    {
                        // 跳过模块对象
                        if (symbolValue is IModuleObject)
                            continue;

                        symbols[symbolName] = symbolValue;
                    }

                    // 使用提取的符号创建模块
                    module = ModuleFactory.CreateModuleFromSymbols(packageName, symbols);
                }
                finally
                {
                    // 清理临时作用域
                    if (manager.Scopes.Count > 0)
                    {
                        manager.Scopes.RemoveAt(manager.Scopes.Count - 1);
                    }
                }
            }
            finally
            {
                manager.Path = previousPath;
            }

            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"    Error loading package '{packageName}': {ex.Message}");
            module = null;
            return false;
        }
    }

    /// <summary>
    /// 查找包的入口文件
    /// </summary>
    private string? FindPackageEntryFile(string packagePath, string packageName)
    {
        // 优先级顺序：
        // 1. index.old8
        // 2. {packageName}.old8
        // 3. main.old8

        var candidates = new[]
        {
            Path.Combine(packagePath, "index.old8"),
            Path.Combine(packagePath, $"{packageName}.old8"),
            Path.Combine(packagePath, "main.old8")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// 检查包是否已安装（委托给 Core 库）
    /// </summary>
    public bool IsPackageInstalled(string packageName)
    {
        return PackageSearchPaths.Any(searchPath =>
        {
            var packagePath = Path.Combine(searchPath, packageName);
            return Directory.Exists(packagePath);
        });
    }

    /// <summary>
    /// 清除包缓存
    /// </summary>
    public void ClearCache()
    {
        lock (LoadLock)
        {
            PackageCache.Clear();
        }
    }

    /// <summary>
    /// 输出调试日志
    /// </summary>
    private static void LogDebug(string message)
    {
        if (DebugEnabled)
        {
            Console.WriteLine($"[PackageManager] {message}");
        }
    }
}