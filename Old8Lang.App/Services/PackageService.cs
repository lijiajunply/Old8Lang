using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Services;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Services;

/// <summary>
/// 包管理服务 - 封装 Old8Lang.PackageManager.Core 功能
/// </summary>
public class PackageService
{
    private readonly string ProjectRoot;
    private readonly string PackagesDir;
    private readonly PackageSourceManager SourceManager;
    private readonly IPackageResolver Resolver;
    private readonly IPackageInstaller Installer;
    private readonly IPackageConfigurationManager ConfigManager;

    /// <summary>
    /// 包目录路径
    /// </summary>
    public string PackagesDirectory => PackagesDir;

    public PackageService(string projectRoot, ProjectConfig? projectConfig = null)
    {
        ProjectRoot = projectRoot;

        // 使用全局包目录
        PackagesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".old8lang",
            "packages"
        );

        // 初始化 Core 库的服务
        SourceManager = new PackageSourceManager();
        Resolver = new DefaultPackageResolver();
        Installer = new DefaultPackageInstaller(SourceManager, Resolver);
        ConfigManager = new DefaultPackageConfigurationManager();

        // 配置包源
        ConfigurePackageSources();
    }

    /// <summary>
    /// 配置包源
    /// </summary>
    private void ConfigurePackageSources()
    {
        // 添加本地包源（项目本地）
        var localPackageSource = new LocalPackageSource(
            name: "Local Packages",
            sourcePath: PackagesDir
        );
        SourceManager.AddSource(localPackageSource);

        // 添加全局包源
        var globalPackagesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".old8lang",
            "packages"
        );
        var globalSource = new LocalPackageSource(
            name: "Global Packages",
            sourcePath: globalPackagesDir
        );
        SourceManager.AddSource(globalSource);

        // TODO: 从项目配置中读取自定义包源
        // TODO: 添加远程包源支持
        SourceManager.AddSource(new LocalPackageSource(
            name: "NuGet",
            sourcePath: "https://api.nuget.org/v3/index.json"
        ));
    }

    /// <summary>
    /// 安装包
    /// </summary>
    private async Task<InstallPackageResult> InstallPackageAsync(
        string packageId,
        string versionRange,
        bool isDevelopmentDependency = false)
    {
        try
        {
            // 解析版本
            var version = ResolveVersion(versionRange);

            // 检查包是否已安装
            var isInstalled = await Installer.IsPackageInstalledAsync(packageId, version, PackagesDir);
            if (isInstalled)
            {
                return new InstallPackageResult
                {
                    Success = true,
                    PackageId = packageId,
                    Version = version,
                    Skipped = true,
                    Message = $"包 '{packageId}@{version}' 已存在"
                };
            }

            // 安装包
            var installResult = await Installer.InstallPackageAsync(packageId, version, PackagesDir);

            if (!installResult.Success)
            {
                return new InstallPackageResult
                {
                    Success = false,
                    PackageId = packageId,
                    Version = version,
                    Message = installResult.Message
                };
            }

            return new InstallPackageResult
            {
                Success = true,
                PackageId = packageId,
                Version = version,
                Message = $"成功安装 {packageId}@{version}"
            };
        }
        catch (Exception ex)
        {
            return new InstallPackageResult
            {
                Success = false,
                PackageId = packageId,
                Message = $"安装失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 安装所有依赖
    /// </summary>
    public async Task<RestoreResult> RestorePackagesAsync(
        ProjectConfig projectConfig,
        bool productionOnly = false)
    {
        var result = new RestoreResult();

        try
        {
            // 安装生产依赖
            var prodDeps = projectConfig.References.Where(r => !r.IsDevDependency).ToList();
            foreach (var dep in prodDeps)
            {
                var version = ResolveVersion(dep.Version);

                var installResult = await InstallPackageAsync(dep.PackageId, version);

                if (installResult.Success)
                {
                    if (installResult.Skipped)
                        result.SkippedCount++;
                    else
                        result.InstalledCount++;
                }
                else
                {
                    result.FailedPackages.Add(dep.PackageId);
                    result.FailedCount++;
                }
            }

            // 安装开发依赖
            if (!productionOnly)
            {
                var devDeps = projectConfig.References.Where(r => r.IsDevDependency).ToList();
                foreach (var dep in devDeps)
                {
                    var version = ResolveVersion(dep.Version);

                    var installResult = await InstallPackageAsync(dep.PackageId, version, true);

                    if (installResult.Success)
                    {
                        if (installResult.Skipped)
                            result.SkippedCount++;
                        else
                            result.InstalledCount++;
                    }
                    else
                    {
                        result.FailedPackages.Add(dep.PackageId);
                        result.FailedCount++;
                    }
                }
            }

            result.Success = result.FailedCount == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 卸载包
    /// </summary>
    public async Task<bool> UninstallPackageAsync(string packageId, string version)
    {
        try
        {
            return await Installer.UninstallPackageAsync(packageId, version, PackagesDir);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取已安装的包列表
    /// </summary>
    public async Task<IEnumerable<Package>> GetInstalledPackagesAsync()
    {
        try
        {
            return await Installer.GetInstalledPackagesAsync(PackagesDir);
        }
        catch
        {
            return Array.Empty<Package>();
        }
    }

    /// <summary>
    /// 解析依赖
    /// </summary>
    public async Task<ResolveResult> ResolveDependenciesAsync(
        string packageId,
        string version)
    {
        return await Resolver.ResolveDependenciesAsync(
            packageId,
            version,
            SourceManager.GetAllSources()
        );
    }

    /// <summary>
    /// 解析版本范围为具体版本
    /// </summary>
    private string ResolveVersion(string versionRange)
    {
        // 简单的版本解析逻辑
        // TODO: 实现完整的语义化版本解析
        var cleanVersion = versionRange.TrimStart('^', '~', '>', '<', '=').Trim();

        if (cleanVersion == "*" || string.IsNullOrWhiteSpace(cleanVersion))
        {
            return "1.0.0"; // 默认版本
        }

        return cleanVersion;
    }
}

/// <summary>
/// 安装包结果
/// </summary>
[Serializable]
public class InstallPackageResult
{
    public bool Success { get; set; }
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool Skipped { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 恢复结果
/// </summary>
[Serializable]
public class RestoreResult
{
    public bool Success { get; set; }
    public int InstalledCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailedPackages { get; set; } = [];
    public string? ErrorMessage { get; set; }
}