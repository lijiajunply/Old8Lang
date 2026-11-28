using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Services;
using Old8Lang.ProjectManagement;
using System.Security.Cryptography.X509Certificates;

namespace Old8Lang.App.Services;

/// <summary>
/// 包管理服务 - 统一通过 Old8Lang.PackageManager.Core 对接唯一远程协议 Old8Lang.PackageManager.Server
/// </summary>
public class PackageService : IDisposable
{
    private readonly string ProjectRoot;
    private readonly PackageSourceManager SourceManager;
    private readonly RemotePackageSource RemoteSource;
    private readonly LocalPackageSource LocalCacheSource;
    private readonly IPackageResolver Resolver;
    private readonly IPackageInstaller Installer;
    private readonly IPackageArchiveService ArchiveService;
    private readonly IPackageSignatureService SignatureService;
    private readonly VersionManager VersionManager;

    /// <summary>
    /// 本地包安装目录（默认 ~/.old8lang/packages）
    /// </summary>
    public string PackagesDir { get; }

    /// <summary>
    /// 当前生效的唯一远程 Old8Lang.PackageManager.Server 地址
    /// </summary>
    public string RegistryUrl { get; }

    public PackageService(
        string projectRoot,
        ProjectConfig? projectConfig = null,
        string? registryOverride = null,
        string? apiKeyOverride = null,
        string? packagesDirOverride = null,
        HttpClient? httpClient = null)
    {
        ProjectRoot = projectRoot;
        PackagesDir = packagesDirOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".old8lang",
            "packages"
        );

        RegistryUrl = GlobalPackageConfig.ResolveRegistryUrl(registryOverride, projectConfig);
        var apiKey = GlobalPackageConfig.ResolveApiKey(apiKeyOverride);

        SourceManager = new PackageSourceManager();
        Resolver = new DefaultPackageResolver();
        ArchiveService = new PackageArchiveService { PackageMetadataFileName = "o8package.json" };
        Installer = new DefaultPackageInstaller(SourceManager, Resolver, ArchiveService);
        SignatureService = new PackageSignatureService();
        VersionManager = new VersionManager();

        // 唯一远程源：Old8Lang.PackageManager.Server（支持通过配置/环境变量/CLI 切换到自建实例）
        RemoteSource = new RemotePackageSource(
            name: "Old8Lang PackageManager",
            source: RegistryUrl,
            apiKey: apiKey,
            timeout: 30,
            language: "old8lang",
            httpClient: httpClient);
        SourceManager.AddSource(RemoteSource);

        // 本地已安装包缓存源（用于离线回退与本地元数据查询）
        LocalCacheSource = new LocalPackageSource(
            name: "Local Cache",
            sourcePath: PackagesDir);
        SourceManager.AddSource(LocalCacheSource);
    }

    /// <summary>
    /// 测试当前配置的远程 Old8Lang.PackageManager.Server 连通性
    /// </summary>
    public Task<bool> TestRegistryConnectionAsync()
    {
        return RemoteSource.TestConnectionAsync();
    }

    /// <summary>
    /// 在远程包仓库搜索包
    /// </summary>
    public Task<IEnumerable<Package>> SearchPackagesAsync(string query, bool includePrerelease = false)
    {
        return RemoteSource.SearchPackagesAsync(query, includePrerelease);
    }

    /// <summary>
    /// 发布 .o8pkg 包文件到当前配置的 Old8Lang.PackageManager.Server
    /// </summary>
    public Task<PackagePublishResult> PublishPackageAsync(
        string packageFilePath,
        PackagePublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RemoteSource.PublishPackageAsync(packageFilePath, options, cancellationToken);
    }

    /// <summary>
    /// 从远程包仓库删除指定版本的包
    /// </summary>
    public Task<bool> DeleteRemotePackageAsync(
        string packageId,
        string version,
        CancellationToken cancellationToken = default)
    {
        return RemoteSource.DeletePackageAsync(packageId, version, cancellationToken);
    }

    /// <summary>
    /// 安装包（自动通过远程包源解析 SemVer 范围，下载并解压到 {PackagesDir}/{packageId}@{version}）
    /// </summary>
    public async Task<InstallPackageResult> InstallPackageAsync(
        string packageId,
        string versionRange,
        bool isDevelopmentDependency = false)
    {
        try
        {
            var version = await ResolveVersionAsync(packageId, versionRange);
            if (string.IsNullOrWhiteSpace(version))
            {
                return new InstallPackageResult
                {
                    Success = false,
                    PackageId = packageId,
                    Version = versionRange,
                    Message = $"在包源 '{RegistryUrl}' 中未找到满足版本范围 '{versionRange}' 的包 '{packageId}'"
                };
            }

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
    /// 恢复项目的所有依赖
    /// </summary>
    public async Task<RestoreResult> RestorePackagesAsync(
        ProjectConfig projectConfig,
        bool productionOnly = false)
    {
        var result = new RestoreResult();

        try
        {
            var prodDeps = projectConfig.References.Where(r => !r.IsDevDependency).ToList();
            foreach (var dep in prodDeps)
            {
                var installResult = await InstallPackageAsync(dep.PackageId, dep.Version);

                if (installResult.Success)
                {
                    if (installResult.Skipped)
                        result.SkippedCount++;
                    else
                        result.InstalledCount++;
                }
                else
                {
                    result.FailedPackages.Add($"{dep.PackageId} ({installResult.Message})");
                    result.FailedCount++;
                }
            }

            if (!productionOnly)
            {
                var devDeps = projectConfig.References.Where(r => r.IsDevDependency).ToList();
                foreach (var dep in devDeps)
                {
                    var installResult = await InstallPackageAsync(dep.PackageId, dep.Version, isDevelopmentDependency: true);

                    if (installResult.Success)
                    {
                        if (installResult.Skipped)
                            result.SkippedCount++;
                        else
                            result.InstalledCount++;
                    }
                    else
                    {
                        result.FailedPackages.Add($"{dep.PackageId} ({installResult.Message})");
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
    /// 卸载指定版本的包
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
    /// 卸载指定包的所有已安装版本
    /// </summary>
    public async Task<int> UninstallAllVersionsAsync(string packageId)
    {
        var installed = (await GetInstalledPackagesAsync())
            .Where(p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var removedCount = 0;
        foreach (var pkg in installed)
        {
            if (await UninstallPackageAsync(pkg.Id, pkg.Version))
            {
                removedCount++;
            }
        }

        return removedCount;
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
            return [];
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
    /// 根据远程和本地可用版本列表解析版本范围为具体版本号
    /// </summary>
    public async Task<string?> ResolveVersionAsync(string packageId, string versionRange)
    {
        var normalizedRange = string.IsNullOrWhiteSpace(versionRange) ? "*" : versionRange.Trim();
        var includePrerelease = normalizedRange.Contains('-') &&
                                !VersionRangeLooksLikeInterval(normalizedRange);

        var availableVersions = new List<string>();

        try
        {
            var remoteVersions = await RemoteSource.GetPackageVersionsAsync(packageId, includePrerelease: true);
            availableVersions.AddRange(remoteVersions);
        }
        catch
        {
            // 远程不可达时允许回退到本地缓存版本
        }

        try
        {
            var localVersions = await LocalCacheSource.GetPackageVersionsAsync(packageId, includePrerelease: true);
            availableVersions.AddRange(localVersions);
        }
        catch
        {
            // 忽略本地缓存异常
        }

        var distinctVersions = availableVersions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctVersions.Count > 0)
        {
            var matched = VersionManager.GetLatestVersionInRange(distinctVersions, normalizedRange, includePrerelease)
                          ?? VersionManager.GetLatestVersionInRange(distinctVersions, normalizedRange, includePrerelease: true);
            if (!string.IsNullOrWhiteSpace(matched))
            {
                return matched;
            }
        }

        // 如果传入的是精确版本号，允许直接尝试该精确版本（兼容仅支持单版本元数据查询的服务端或本地缓存）
        if (IsExactVersion(normalizedRange))
        {
            return normalizedRange;
        }

        return null;
    }

    private static bool IsExactVersion(string versionSpec)
    {
        if (string.IsNullOrWhiteSpace(versionSpec))
            return false;

        if (versionSpec == "*" ||
            versionSpec.Equals("latest", StringComparison.OrdinalIgnoreCase) ||
            versionSpec.StartsWith('^') ||
            versionSpec.StartsWith('~') ||
            versionSpec.StartsWith('>') ||
            versionSpec.StartsWith('<') ||
            versionSpec.Contains('*') ||
            VersionRangeLooksLikeInterval(versionSpec))
        {
            return false;
        }

        return char.IsDigit(versionSpec[0]) && versionSpec.Contains('.');
    }

    private static bool VersionRangeLooksLikeInterval(string versionRange)
    {
        if (versionRange.Contains(" - ", StringComparison.Ordinal))
            return true;

        var dashIndex = versionRange.IndexOf('-');
        if (dashIndex > 0 && dashIndex < versionRange.Length - 1)
        {
            var right = versionRange[(dashIndex + 1)..].Trim();
            return right.Length > 0 && char.IsDigit(right[0]) && right.Contains('.');
        }

        return false;
    }

    #region 打包功能

    public Task<string> PackAsync(
        string sourcePath,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        return ArchiveService.PackAsync(sourcePath, outputPath, cancellationToken);
    }

    public Task UnpackAsync(
        string packagePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        return ArchiveService.UnpackAsync(packagePath, destinationPath, cancellationToken);
    }

    public Task<(bool IsValid, string Message)> ValidatePackageStructureAsync(string sourcePath)
    {
        return ArchiveService.ValidatePackageStructureAsync(sourcePath);
    }

    public Task<Package?> ReadPackageMetadataAsync(string sourcePath)
    {
        return ArchiveService.ReadPackageMetadataAsync(sourcePath);
    }

    #endregion

    #region 签名功能

    public Task<PackageSignature> SignPackageAsync(
        string packagePath,
        X509Certificate2 certificate)
    {
        return SignatureService.SignPackageAsync(packagePath, certificate);
    }

    public Task<bool> VerifySignatureAsync(
        string packagePath,
        PackageSignature signature)
    {
        return SignatureService.VerifySignatureAsync(packagePath, signature);
    }

    public Task<PackageSignature?> ReadSignatureAsync(string signatureFilePath)
    {
        return SignatureService.ReadSignatureAsync(signatureFilePath);
    }

    public Task WriteSignatureAsync(
        PackageSignature signature,
        string signatureFilePath)
    {
        return SignatureService.WriteSignatureAsync(signature, signatureFilePath);
    }

    public X509Certificate2 GenerateSelfSignedCertificate(
        string subjectName,
        string? email = null,
        int validityYears = 5)
    {
        return SignatureService.GenerateSelfSignedCertificate(subjectName, email, validityYears);
    }

    public Task<X509Certificate2> LoadCertificateAsync(
        string certPath,
        string? password = null)
    {
        return SignatureService.LoadCertificateAsync(certPath, password);
    }

    public Task ExportCertificateAsync(
        X509Certificate2 certificate,
        string outputPath,
        string? password = null)
    {
        return SignatureService.ExportCertificateAsync(certificate, outputPath, password);
    }

    public string GetCertificateInfo(X509Certificate2 certificate)
    {
        return SignatureService.GetCertificateInfo(certificate);
    }

    #endregion

    public void Dispose()
    {
        RemoteSource.Dispose();
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
