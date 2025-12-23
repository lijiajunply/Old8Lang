using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.PackageManagement;

/// <summary>
/// 第三方包管理器，负责加载和管理第三方 Old8Lang 包
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
    /// 构造函数
    /// </summary>
    /// <param name="packagesDir">包目录路径，null 则使用默认路径</param>
    public PackageManager(string? packagesDir = null)
    {
        var packagesDirectory = packagesDir ?? GetDefaultPackagesDirectory();

        // 添加默认查找路径
        AddSearchPath(packagesDirectory);

        // 添加当前目录的 packages 子目录
        var localPackages = Path.Combine(Directory.GetCurrentDirectory(), "packages");
        if (Directory.Exists(localPackages))
        {
            AddSearchPath(localPackages);
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
        if (!PackageSearchPaths.Contains(path))
        {
            PackageSearchPaths.Add(path);
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
            // 检查缓存
            if (PackageCache.TryGetValue(packageName, out module))
            {
                return true;
            }

            // 在所有查找路径中搜索包
            foreach (var packagePath in PackageSearchPaths.Select(searchPath => Path.Combine(searchPath, packageName)))
            {
                if (!TryLoadPackageFromPath(packagePath, packageName, manager, out module) || module is null) continue;
                PackageCache[packageName] = module;
                return true;
            }

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
            module = null;
            return false;
        }

        try
        {
            // 查找包的入口文件
            var entryFile = FindPackageEntryFile(packagePath, packageName);

            if (entryFile == null)
            {
                module = null;
                return false;
            }

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
            throw new Old8Exception("PACKAGE_LOAD_ERROR", $"加载包 '{packageName}' 时出错: {ex.Message}",
                new SourcePosition(), null, null, null, null, ex);
        }
    }

    /// <summary>
    /// 查找包的入口文件
    /// </summary>
    private string? FindPackageEntryFile(string packagePath, string packageName)
    {
        // 优先级顺序：
        // 1. package.json 中指定的 main 字段
        // 2. index.old8
        // 3. {packageName}.old8
        // 4. main.old8

        // 尝试读取 package.json
        var packageJsonPath = Path.Combine(packagePath, "package.json");
        if (File.Exists(packageJsonPath))
        {
            try
            {
                var json = File.ReadAllText(packageJsonPath);
                // 简单的 JSON 解析查找 "main" 字段
                var mainMatch = System.Text.RegularExpressions.Regex.Match(
                    json,
                    @"""main""\s*:\s*""([^""]+)"""
                );

                if (mainMatch.Success)
                {
                    var mainFile = Path.Combine(packagePath, mainMatch.Groups[1].Value);
                    if (File.Exists(mainFile))
                        return mainFile;
                }
            }
            catch
            {
                // 解析失败，继续尝试其他方式
            }
        }

        // 尝试常见的入口文件名
        var candidates = new[]
        {
            Path.Combine(packagePath, "index.old8"),
            Path.Combine(packagePath, $"{packageName}.old8"),
            Path.Combine(packagePath, "main.old8")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// 检查包是否已安装
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
    /// 获取已安装的包列表
    /// </summary>
    public IEnumerable<string> GetInstalledPackages()
    {
        var packages = new HashSet<string>();

        foreach (var searchPath in PackageSearchPaths)
        {
            if (!Directory.Exists(searchPath))
                continue;

            var directories = Directory.GetDirectories(searchPath);
            foreach (var dir in directories)
            {
                var packageName = Path.GetFileName(dir);
                packages.Add(packageName);
            }
        }

        return packages;
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
    /// 获取包信息
    /// </summary>
    public PackageInfo? GetPackageInfo(string packageName)
    {
        foreach (var searchPath in PackageSearchPaths)
        {
            var packagePath = Path.Combine(searchPath, packageName);
            var packageJsonPath = Path.Combine(packagePath, "package.json");

            if (!File.Exists(packageJsonPath)) continue;
            try
            {
                var json = File.ReadAllText(packageJsonPath);
                return PackageInfo.FromJson(json, packagePath);
            }
            catch
            {
                //
            }
        }

        return null;
    }
}

/// <summary>
/// 包信息
/// </summary>
public class PackageInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Main { get; set; }
    public string Path { get; set; } = "";

    public static PackageInfo FromJson(string json, string packagePath)
    {
        // 简单的 JSON 解析
        var info = new PackageInfo { Path = packagePath };

        var nameMatch = System.Text.RegularExpressions.Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
        if (nameMatch.Success)
            info.Name = nameMatch.Groups[1].Value;

        var versionMatch = System.Text.RegularExpressions.Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
        if (versionMatch.Success)
            info.Version = versionMatch.Groups[1].Value;

        var descMatch = System.Text.RegularExpressions.Regex.Match(json, @"""description""\s*:\s*""([^""]+)""");
        if (descMatch.Success)
            info.Description = descMatch.Groups[1].Value;

        var mainMatch = System.Text.RegularExpressions.Regex.Match(json, @"""main""\s*:\s*""([^""]+)""");
        if (mainMatch.Success)
            info.Main = mainMatch.Groups[1].Value;

        return info;
    }
}