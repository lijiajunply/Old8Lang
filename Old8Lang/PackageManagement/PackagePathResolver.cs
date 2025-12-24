using System.Text.Json;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManagement;

/// <summary>
/// 包路径解析器，负责解析导入语句中的路径（文件路径或包路径）
/// </summary>
public class PackagePathResolver
{
    private readonly string _projectRoot;
    private readonly string _packagesDir;
    private readonly PackageConfiguration? _config;

    /// <summary>
    /// 创建包路径解析器
    /// </summary>
    /// <param name="projectRoot">项目根目录</param>
    public PackagePathResolver(string projectRoot)
    {
        _projectRoot = projectRoot;
        _packagesDir = Path.Combine(projectRoot, "packages");
        _config = LoadPackageConfiguration();
    }

    /// <summary>
    /// 解析导入路径（文件路径或包路径）
    /// </summary>
    /// <param name="importPath">导入路径字符串</param>
    /// <param name="currentFile">当前文件的绝对路径（用于解析相对路径）</param>
    /// <returns>解析后的绝对文件路径，如果无法解析则返回 null</returns>
    public string? ResolveImportPath(string importPath, string? currentFile = null)
    {
        // 1. 检查是否为文件路径
        if (IsFilePath(importPath))
        {
            return ResolveFilePath(importPath, currentFile);
        }

        // 2. 检查是否为包导入
        if (IsPackageImport(importPath))
        {
            return ResolvePackagePath(importPath);
        }

        return null;
    }

    /// <summary>
    /// 判断是否为文件路径
    /// </summary>
    private bool IsFilePath(string path)
    {
        return path.StartsWith(".")         // ./file.o8 或 ../file.o8
            || path.StartsWith("/")         // /abs/path/file.o8
            || path.Contains(":\\")         // C:\path\file.o8 (Windows)
            || path.EndsWith(".o8");        // relative/file.o8
    }

    /// <summary>
    /// 判断是否为包导入
    /// </summary>
    private bool IsPackageImport(string path)
    {
        return !IsFilePath(path);
    }

    /// <summary>
    /// 解析文件路径
    /// </summary>
    private string? ResolveFilePath(string filePath, string? currentFile)
    {
        // 如果是绝对路径，直接返回
        if (Path.IsPathRooted(filePath))
        {
            return File.Exists(filePath) ? filePath : null;
        }

        // 相对路径：相对于当前文件
        if (currentFile != null)
        {
            var currentDir = Path.GetDirectoryName(currentFile);
            if (currentDir != null)
            {
                var absolutePath = Path.GetFullPath(Path.Combine(currentDir, filePath));
                if (File.Exists(absolutePath))
                {
                    return absolutePath;
                }
            }
        }

        // 相对路径：相对于项目根目录
        var projectPath = Path.GetFullPath(Path.Combine(_projectRoot, filePath));
        if (File.Exists(projectPath))
        {
            return projectPath;
        }

        return null;
    }

    /// <summary>
    /// 解析包路径
    /// </summary>
    private string? ResolvePackagePath(string packageImport)
    {
        // 解析包名、版本和子路径
        var (packageName, version, subPath) = ParsePackageImport(packageImport);

        // 如果没有指定版本，从配置文件获取版本
        if (version == null && _config != null)
        {
            var reference = _config.References
                .FirstOrDefault(r => r.PackageId == packageName);
            version = reference?.Version;
        }

        if (version == null)
        {
            throw new Exception($"Package '{packageName}' not found in o8packages.json. Please add it first using: old8lang package add {packageName}");
        }

        // 构建包路径
        var packageDir = Path.Combine(_packagesDir, $"{packageName}.{version}");

        // 检查包目录是否存在
        if (!Directory.Exists(packageDir))
        {
            throw new Exception($"Package '{packageName}' version '{version}' is not installed. Run 'old8lang package restore' to install dependencies.");
        }

        var framework = _config?.Framework ?? "old8lang-1.0";
        var libDir = Path.Combine(packageDir, "lib", framework);

        // 检查库目录是否存在
        if (!Directory.Exists(libDir))
        {
            throw new Exception($"Package '{packageName}' does not support framework '{framework}'");
        }

        // 查找主模块或子模块
        string targetFile;
        if (string.IsNullOrEmpty(subPath))
        {
            // 主模块：PackageName.o8
            targetFile = Path.Combine(libDir, $"{packageName}.o8");
        }
        else
        {
            // 子模块：SubPath.o8
            var subPathWithExtension = subPath.EndsWith(".o8") ? subPath : $"{subPath}.o8";
            targetFile = Path.Combine(libDir, subPathWithExtension);
        }

        if (!File.Exists(targetFile))
        {
            var expectedLocation = Path.GetRelativePath(_projectRoot, targetFile);
            throw new Exception($"Module not found: {expectedLocation}\nPackage: {packageName}@{version}");
        }

        return targetFile;
    }

    /// <summary>
    /// 解析包导入语句
    /// </summary>
    /// <param name="import">导入字符串</param>
    /// <returns>包名、版本（可选）、子路径（可选）</returns>
    /// <example>
    /// "Logger@1.2.0" -> ("Logger", "1.2.0", null)
    /// "Utils/StringHelper" -> ("Utils", null, "StringHelper")
    /// "Logger" -> ("Logger", null, null)
    /// </example>
    private (string packageName, string? version, string? subPath) ParsePackageImport(string import)
    {
        string packageName;
        string? version = null;
        string? subPath = null;

        // 处理版本（使用 @ 符号）
        var versionIndex = import.IndexOf('@');
        if (versionIndex > 0)
        {
            packageName = import.Substring(0, versionIndex);
            version = import.Substring(versionIndex + 1);

            // 版本中可能还有子路径
            var pathInVersion = version.IndexOf('/');
            if (pathInVersion > 0)
            {
                subPath = version.Substring(pathInVersion + 1);
                version = version.Substring(0, pathInVersion);
            }
        }
        else
        {
            // 处理子路径（使用 / 分隔符）
            var pathIndex = import.IndexOf('/');
            if (pathIndex > 0)
            {
                packageName = import.Substring(0, pathIndex);
                subPath = import.Substring(pathIndex + 1);
            }
            else
            {
                packageName = import;
            }
        }

        return (packageName, version, subPath);
    }

    /// <summary>
    /// 加载 o8packages.json 配置文件
    /// </summary>
    private PackageConfiguration? LoadPackageConfiguration()
    {
        var configPath = Path.Combine(_projectRoot, "o8packages.json");
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<PackageConfiguration>(json, options);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse o8packages.json: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取包安装目录
    /// </summary>
    public string PackagesDirectory => _packagesDir;

    /// <summary>
    /// 获取项目根目录
    /// </summary>
    public string ProjectRoot => _projectRoot;

    /// <summary>
    /// 获取包配置
    /// </summary>
    public PackageConfiguration? Configuration => _config;
}
