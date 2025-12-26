using Old8Lang.StandardLibrary;
using Old8Lang.PackageManagement;
using Old8Lang.Interpreter;

namespace Old8Lang.ModuleSystem.Resolution;

/// <summary>
/// 模块解析结果
/// </summary>
public class ModuleResolutionResult
{
    /// <summary>
    /// 解析后的模块路径（绝对路径）
    /// </summary>
    public string? ResolvedPath { get; set; }

    /// <summary>
    /// 模块类型
    /// </summary>
    public ModuleType ModuleType { get; set; }

    /// <summary>
    /// 是否解析成功
    /// </summary>
    public bool IsSuccess => ResolvedPath != null;

    /// <summary>
    /// 尝试过的路径列表（用于错误报告）
    /// </summary>
    public List<string> AttemptedPaths { get; set; } = new();

    /// <summary>
    /// 包信息（如果是第三方包）
    /// </summary>
    public PackageInfo? PackageInfo { get; set; }
}

/// <summary>
/// 模块类型枚举
/// </summary>
public enum ModuleType
{
    /// <summary>
    /// 标准库模块
    /// </summary>
    StandardLibrary,

    /// <summary>
    /// 第三方包
    /// </summary>
    ThirdPartyPackage,

    /// <summary>
    /// 本地文件
    /// </summary>
    LocalFile,

    /// <summary>
    /// 网络模块（URL）
    /// </summary>
    NetworkModule,

    /// <summary>
    /// 子模块（module.submodule）
    /// </summary>
    Submodule,

    /// <summary>
    /// 未知
    /// </summary>
    Unknown
}

/// <summary>
/// 包信息
/// </summary>
public class PackageInfo
{
    public string PackageName { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string PackagePath { get; set; } = string.Empty;
}

/// <summary>
/// 模块解析器 - 负责解析模块名称到实际路径
/// 整合了标准库、第三方包和本地文件的查找逻辑
/// </summary>
public class ModuleResolver
{
    private readonly PathResolver _pathResolver;
    private readonly VersionResolver _versionResolver;

    public ModuleResolver()
    {
        _pathResolver = new PathResolver();
        _versionResolver = new VersionResolver();
    }

    /// <summary>
    /// 解析模块
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="currentFilePath">当前文件路径</param>
    /// <param name="manager">变量管理器（可选）</param>
    /// <returns>模块解析结果</returns>
    public ModuleResolutionResult ResolveModule(string moduleName, string? currentFilePath, VariateManager? manager = null)
    {
        var result = new ModuleResolutionResult();

        // 1. 检查是否为网络路径
        if (_pathResolver.IsUrl(moduleName))
        {
            result.ModuleType = ModuleType.NetworkModule;
            result.ResolvedPath = moduleName;
            return result;
        }

        // 2. 检查是否为子模块语法（module.submodule）
        if (IsSubmoduleSyntax(moduleName))
        {
            result.ModuleType = ModuleType.Submodule;
            var submodulePath = ResolveSubmodule(moduleName, currentFilePath, manager);
            result.ResolvedPath = submodulePath;
            if (submodulePath != null)
            {
                result.AttemptedPaths.Add(submodulePath);
            }
            return result;
        }

        // 3. 优先级 1: 标准库
        if (StandardLibraryRegistry.IsStandardLibrary(moduleName))
        {
            result.ModuleType = ModuleType.StandardLibrary;
            result.ResolvedPath = moduleName; // 标准库使用名称作为标识
            return result;
        }

        // 4. 优先级 2: 第三方包
        // 跳过相对路径和绝对路径
        if (!moduleName.StartsWith("./") && !moduleName.StartsWith("../") && !Path.IsPathRooted(moduleName))
        {
            // 解析版本
            _versionResolver.ParsePackageSpec(moduleName, out var packageName, out var versionSpec);

            var packagePath = ResolvePackage(packageName, versionSpec, currentFilePath);
            if (packagePath != null)
            {
                result.ModuleType = ModuleType.ThirdPartyPackage;
                result.ResolvedPath = packagePath;
                result.PackageInfo = new PackageInfo
                {
                    PackageName = packageName,
                    Version = versionSpec,
                    PackagePath = packagePath
                };
                result.AttemptedPaths.Add(packagePath);
                return result;
            }
        }

        // 5. 优先级 3: 本地文件
        var localPath = ResolveLocalFile(moduleName, currentFilePath, result.AttemptedPaths);
        if (localPath != null)
        {
            result.ModuleType = ModuleType.LocalFile;
            result.ResolvedPath = localPath;
        }

        return result;
    }

    /// <summary>
    /// 检查是否为子模块语法
    /// </summary>
    private bool IsSubmoduleSyntax(string moduleName)
    {
        return moduleName.Contains('.') &&
               !moduleName.StartsWith("./") &&
               !moduleName.StartsWith("../") &&
               !moduleName.EndsWith(".old8") &&
               !moduleName.EndsWith(".ol");
    }

    /// <summary>
    /// 解析子模块
    /// </summary>
    private string? ResolveSubmodule(string moduleName, string? currentFilePath, VariateManager? manager)
    {
        var parts = moduleName.Split('.');

        // 确定基础路径
        string basePath;
        if (moduleName.StartsWith("./") || moduleName.StartsWith("../"))
        {
            // 相对路径
            var currentFileDir = Path.GetDirectoryName(currentFilePath);
            basePath = string.IsNullOrEmpty(currentFileDir) ? Directory.GetCurrentDirectory() : currentFileDir;
        }
        else
        {
            // 使用 ImportPath 或当前工作目录
            basePath = manager?.LangInfo?.ImportPath ?? Directory.GetCurrentDirectory();
        }

        var currentPath = basePath;

        // 逐级查找子模块
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var testPath = Path.Combine(currentPath, part);

            if (i == parts.Length - 1)
            {
                // 最后一个部分，查找文件或目录
                var filePath = testPath + ".old8";
                if (File.Exists(filePath))
                {
                    return filePath;
                }

                // 查找目录中的 __init__.old8 或 index.old8
                if (Directory.Exists(testPath))
                {
                    var initFile = Path.Combine(testPath, "__init__.old8");
                    if (File.Exists(initFile))
                    {
                        return initFile;
                    }

                    var indexFile = Path.Combine(testPath, "index.old8");
                    if (File.Exists(indexFile))
                    {
                        return indexFile;
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
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// 解析第三方包
    /// </summary>
    private string? ResolvePackage(string packageName, string? versionSpec, string? currentFilePath)
    {
        var searchPaths = GetPackageSearchPaths(currentFilePath);

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
            {
                continue;
            }

            try
            {
                // 策略 1: 精确目录名（无版本）
                var packagePath = Path.Combine(searchPath, packageName);
                var entryFile = FindPackageEntryFile(packagePath, packageName);
                if (entryFile != null)
                {
                    return entryFile;
                }

                // 策略 2: 版本化目录
                var versionedDirs = Directory.GetDirectories(searchPath, $"{packageName}@*");
                if (versionedDirs.Length > 0)
                {
                    // 选择最佳匹配版本
                    var bestMatch = _versionResolver.SelectBestVersion(
                        versionedDirs.Select(Path.GetFileName).Where(x => x != null).Cast<string>(),
                        versionSpec
                    );

                    if (bestMatch != null)
                    {
                        var versionedPath = Path.Combine(searchPath, bestMatch);
                        entryFile = FindPackageEntryFile(versionedPath, packageName);
                        if (entryFile != null)
                        {
                            return entryFile;
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误，继续尝试下一个路径
            }
        }

        return null;
    }

    /// <summary>
    /// 获取包搜索路径列表
    /// </summary>
    private List<string> GetPackageSearchPaths(string? currentFilePath)
    {
        var searchPaths = new List<string>();

        // 全局包目录
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalPackagesDir = Path.Combine(homeDir, ".old8lang", "packages");
        searchPaths.Add(globalPackagesDir);

        // 本地包目录
        if (!string.IsNullOrEmpty(currentFilePath))
        {
            try
            {
                var sourceDir = Path.GetDirectoryName(Path.GetFullPath(currentFilePath));
                if (!string.IsNullOrEmpty(sourceDir))
                {
                    var localPackages = Path.Combine(sourceDir, "packages");
                    if (Directory.Exists(localPackages))
                    {
                        searchPaths.Insert(0, localPackages); // 本地包优先级更高
                    }
                }
            }
            catch
            {
                // 忽略路径解析错误
            }
        }

        return searchPaths;
    }

    /// <summary>
    /// 查找包的入口文件
    /// </summary>
    private string? FindPackageEntryFile(string packagePath, string packageName)
    {
        if (!Directory.Exists(packagePath))
        {
            return null;
        }

        // 优先级顺序
        var candidates = new[]
        {
            Path.Combine(packagePath, "index.old8"),
            Path.Combine(packagePath, $"{packageName}.old8"),
            Path.Combine(packagePath, "main.old8")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// 解析本地文件
    /// </summary>
    private string? ResolveLocalFile(string moduleName, string? currentFilePath, List<string> attemptedPaths)
    {
        // 添加扩展名（如果需要）
        var fileName = _pathResolver.EnsureExtension(moduleName);

        // 解析路径
        var filePath = _pathResolver.ResolvePath(fileName, currentFilePath);

        attemptedPaths.Add(filePath);

        // 检查文件是否存在
        if (File.Exists(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        return null;
    }
}
