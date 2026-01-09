using Old8Lang.StandardLibrary;
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
    public bool IsSuccess => ResolvedPath is not null;

    /// <summary>
    /// 尝试过的路径列表（用于错误报告）
    /// </summary>
    public List<string> AttemptedPaths { get; set; } = new();

    /// <summary>
    /// 包信息（如果是第三方包）
    /// </summary>
    public PackageInfo? PackageInfo { get; set; }

    /// <summary>
    /// 失败原因（当 IsSuccess 为 false 时）
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// 失败详情列表（记录每个尝试步骤的详细信息）
    /// </summary>
    public List<ResolutionAttempt> ResolutionAttempts { get; set; } = new();

    /// <summary>
    /// 获取友好的错误信息
    /// </summary>
    public string GetFriendlyErrorMessage(string moduleName)
    {
        var lines = new List<string>
        {
            $"无法导入模块 '{moduleName}'"
        };

        if (!string.IsNullOrEmpty(FailureReason))
        {
            lines.Add("");
            lines.Add($"原因: {FailureReason}");
        }

        if (ResolutionAttempts.Count > 0)
        {
            lines.Add("");
            lines.Add("尝试的解析步骤:");
            foreach (var attempt in ResolutionAttempts)
            {
                lines.Add($"  [{attempt.StepNumber}] {attempt.Description}");
                if (!string.IsNullOrEmpty(attempt.SearchPath))
                {
                    lines.Add($"      路径: {attempt.SearchPath}");
                }
                if (!string.IsNullOrEmpty(attempt.Result))
                {
                    lines.Add($"      结果: {attempt.Result}");
                }
            }
        }
        else if (AttemptedPaths.Count > 0)
        {
            lines.Add("");
            lines.Add("尝试的路径:");
            foreach (var path in AttemptedPaths)
            {
                lines.Add($"  - {path}");
            }
        }

        // 添加建议
        lines.Add("");
        lines.Add("建议:");
        lines.AddRange(GenerateSuggestions(moduleName));

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// 生成针对性建议
    /// </summary>
    private List<string> GenerateSuggestions(string moduleName)
    {
        var suggestions = new List<string>();

        // 根据模块名称和解析过程提供建议
        if (moduleName.Contains('.') && !moduleName.EndsWith(".old8"))
        {
            suggestions.Add("  - 如果这是子模块导入，请确认父模块目录包含 __init__.old8 或 index.old8");
        }

        if (!moduleName.StartsWith("./") && !moduleName.StartsWith("../") && !Path.IsPathRooted(moduleName))
        {
            suggestions.Add("  - 如果这是第三方包，请确认已安装该包到 ~/.old8lang/packages/ 或当前目录的 packages/");
            suggestions.Add("  - 如果这是本地文件，请使用相对路径 (如 './module' 或 '../module')");
        }

        suggestions.Add("  - 检查模块名称拼写是否正确");
        suggestions.Add("  - 确认模块文件扩展名为 .old8");

        // 检查是否可能是标准库名称拼写错误
        var similarStandardLibs = FindSimilarStandardLibraries(moduleName);
        if (similarStandardLibs.Any())
        {
            suggestions.Add($"  - 您是否想导入以下标准库之一: {string.Join(", ", similarStandardLibs)}");
        }

        return suggestions;
    }

    /// <summary>
    /// 查找相似的标准库名称
    /// </summary>
    private List<string> FindSimilarStandardLibraries(string moduleName)
    {
        var allLibs = StandardLibraryRegistry.GetAllLibraryNames();
        var similar = new List<string>();

        var lowerModuleName = moduleName.ToLowerInvariant();
        foreach (var lib in allLibs)
        {
            var lowerLib = lib.ToLowerInvariant();
            // 简单的相似度检测：包含关系或前缀匹配
            if (lowerLib.Contains(lowerModuleName) || lowerModuleName.Contains(lowerLib) ||
                lowerLib.StartsWith(lowerModuleName.Substring(0, Math.Min(3, lowerModuleName.Length))))
            {
                similar.Add(lib);
            }
        }

        return similar.Take(3).ToList(); // 最多返回3个建议
    }
}

/// <summary>
/// 解析尝试记录
/// </summary>
public class ResolutionAttempt
{
    /// <summary>
    /// 步骤编号
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// 步骤描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 搜索路径
    /// </summary>
    public string? SearchPath { get; set; }

    /// <summary>
    /// 结果描述
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
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
    private readonly PathResolver PathResolver = new();
    private readonly VersionResolver VersionResolver = new();

    /// <summary>
    /// 解析模块
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="currentFilePath">当前文件路径</param>
    /// <param name="manager">变量管理器（可选）</param>
    /// <returns>模块解析结果</returns>
    public ModuleResolutionResult ResolveModule(string moduleName, string? currentFilePath,
        VariateManager? manager = null)
    {
        var result = new ModuleResolutionResult();
        int stepNumber = 0;

        // 1. 检查是否为网络路径
        stepNumber++;
        result.ResolutionAttempts.Add(new ResolutionAttempt
        {
            StepNumber = stepNumber,
            Description = "检查是否为网络路径 (http:// 或 https://)",
            SearchPath = moduleName,
            Result = PathResolver.IsUrl(moduleName) ? "是网络路径" : "不是网络路径",
            Success = PathResolver.IsUrl(moduleName)
        });

        if (PathResolver.IsUrl(moduleName))
        {
            result.ModuleType = ModuleType.NetworkModule;
            result.ResolvedPath = moduleName;
            return result;
        }

        // 2. 检查是否为子模块语法（module.submodule）
        stepNumber++;
        var isSubmodule = IsSubmoduleSyntax(moduleName);
        result.ResolutionAttempts.Add(new ResolutionAttempt
        {
            StepNumber = stepNumber,
            Description = "检查是否为子模块语法 (module.submodule)",
            SearchPath = moduleName,
            Result = isSubmodule ? "是子模块语法，尝试解析" : "不是子模块语法",
            Success = false
        });

        if (isSubmodule)
        {
            result.ModuleType = ModuleType.Submodule;
            var submodulePath = ResolveSubmodule(moduleName, currentFilePath, manager);
            result.ResolvedPath = submodulePath;
            if (submodulePath is not null)
            {
                result.AttemptedPaths.Add(submodulePath);
                result.ResolutionAttempts[^1].Result = $"找到子模块: {submodulePath}";
                result.ResolutionAttempts[^1].Success = true;
            }
            else
            {
                result.ResolutionAttempts[^1].Result = "子模块解析失败";
                result.FailureReason = "子模块路径不存在或缺少 __init__.old8 / index.old8 文件";
            }

            return result;
        }

        // 3. 优先级 1: 标准库
        stepNumber++;
        var isStandardLib = StandardLibraryRegistry.IsStandardLibrary(moduleName);
        result.ResolutionAttempts.Add(new ResolutionAttempt
        {
            StepNumber = stepNumber,
            Description = "检查是否为标准库",
            SearchPath = moduleName,
            Result = isStandardLib ? $"找到标准库: {moduleName}" : "不是标准库",
            Success = isStandardLib
        });

        if (isStandardLib)
        {
            result.ModuleType = ModuleType.StandardLibrary;
            result.ResolvedPath = moduleName; // 标准库使用名称作为标识
            return result;
        }

        // 4. 优先级 2: 第三方包
        // 跳过相对路径和绝对路径
        if (!moduleName.StartsWith("./") && !moduleName.StartsWith("../") && !Path.IsPathRooted(moduleName))
        {
            stepNumber++;
            // 解析版本
            VersionResolver.ParsePackageSpec(moduleName, out var packageName, out var versionSpec);

            var packageSearchPaths = GetPackageSearchPaths(currentFilePath);
            result.ResolutionAttempts.Add(new ResolutionAttempt
            {
                StepNumber = stepNumber,
                Description = $"在包目录中搜索第三方包 '{packageName}'" +
                             (versionSpec is not null ? $" (版本: {versionSpec})" : ""),
                SearchPath = string.Join(", ", packageSearchPaths),
                Result = "正在搜索...",
                Success = false
            });

            var packagePath = ResolvePackage(packageName, versionSpec, currentFilePath);
            if (packagePath is not null)
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
                result.ResolutionAttempts[^1].Result = $"找到包: {packagePath}";
                result.ResolutionAttempts[^1].Success = true;
                return result;
            }
            else
            {
                result.ResolutionAttempts[^1].Result = "未找到匹配的包";
            }
        }

        // 5. 优先级 3: 本地文件
        stepNumber++;
        result.ResolutionAttempts.Add(new ResolutionAttempt
        {
            StepNumber = stepNumber,
            Description = "尝试解析为本地文件",
            SearchPath = currentFilePath is not null
                ? $"相对于: {Path.GetDirectoryName(currentFilePath) ?? "当前目录"}"
                : "相对于: 当前工作目录",
            Result = "正在搜索...",
            Success = false
        });

        var localPath = ResolveLocalFile(moduleName, currentFilePath, result.AttemptedPaths);
        if (localPath is not null)
        {
            result.ModuleType = ModuleType.LocalFile;
            result.ResolvedPath = localPath;
            result.ResolutionAttempts[^1].Result = $"找到文件: {localPath}";
            result.ResolutionAttempts[^1].Success = true;
        }
        else
        {
            result.ResolutionAttempts[^1].Result = $"文件不存在: {result.AttemptedPaths.LastOrDefault() ?? moduleName}";
            result.FailureReason = "模块文件不存在";
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
                if (entryFile is not null)
                {
                    return entryFile;
                }

                // 策略 2: 版本化目录
                var versionedDirs = Directory.GetDirectories(searchPath, $"{packageName}@*");
                if (versionedDirs.Length > 0)
                {
                    // 选择最佳匹配版本
                    var bestMatch = VersionResolver.SelectBestVersion(
                        versionedDirs.Select(Path.GetFileName).Where(x => x is not null).Cast<string>(),
                        versionSpec
                    );

                    if (bestMatch is not null)
                    {
                        var versionedPath = Path.Combine(searchPath, bestMatch);
                        entryFile = FindPackageEntryFile(versionedPath, packageName);
                        if (entryFile is not null)
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
        var fileName = PathResolver.EnsureExtension(moduleName);

        // 解析路径
        var filePath = PathResolver.ResolvePath(fileName, currentFilePath);

        attemptedPaths.Add(filePath);

        // 检查文件是否存在
        if (File.Exists(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        return null;
    }
}