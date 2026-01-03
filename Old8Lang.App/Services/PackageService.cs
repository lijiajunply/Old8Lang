using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Services;
using Old8Lang.ProjectManagement;
using System.Security.Cryptography.X509Certificates;

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
    private readonly IPackageArchiveService ArchiveService;
    private readonly IPackageSignatureService SignatureService;
    private readonly VersionManager VersionManager;

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
        new DefaultPackageConfigurationManager();
        ArchiveService = new PackageArchiveService() { PackageMetadataFileName = "o8package.json" };
        SignatureService = new PackageSignatureService();
        VersionManager = new VersionManager();

        // 配置包源
        ConfigurePackageSources(projectConfig);
    }

    /// <summary>
    /// 配置包源
    /// </summary>
    private void ConfigurePackageSources(ProjectConfig? projectConfig)
    {
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

        // 从项目配置中读取自定义包源
        projectConfig?.Sources.ForEach(x => SourceManager.AddSource(new RemotePackageSource(x.Name, source: x.Source)));

        // 添加远程包源支持
        SourceManager.AddSource(new LocalPackageSource(
            name: "Old8Lang Web",
            sourcePath: "https://package.old8lang.site/v3"
        ));
    }

    /// <summary>
    /// 安装包
    /// </summary>
    public async Task<InstallPackageResult> InstallPackageAsync(
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
    /// <param name="versionRange">版本范围字符串（支持 ^, ~, *, >, &lt;, >=, &lt;=, 精确版本, 范围）</param>
    /// <returns>解析后的版本字符串</returns>
    private string ResolveVersion(string versionRange)
    {
        if (string.IsNullOrWhiteSpace(versionRange))
        {
            return "1.0.0"; // 默认版本
        }

        versionRange = versionRange.Trim();

        // 处理通配符 * - 返回默认版本
        if (versionRange == "*")
        {
            return "1.0.0";
        }

        // 处理 npm 风格的版本范围
        // ^1.2.3 - 兼容 1.x.x 版本（主版本相同）
        if (versionRange.StartsWith("^"))
        {
            var baseVersion = versionRange[1..].Trim();
            var parsed = VersionManager.ParseVersion(baseVersion);
            // 对于 ^ 语义，返回基准版本
            return parsed.ToString();
        }

        // ~1.2.3 - 兼容 1.2.x 版本（主版本和次版本相同）
        if (versionRange.StartsWith("~"))
        {
            var baseVersion = versionRange[1..].Trim();
            var parsed = VersionManager.ParseVersion(baseVersion);
            // 对于 ~ 语义，返回基准版本
            return parsed.ToString();
        }

        // 处理比较运算符版本范围 (>=, <=, >, <)
        if (versionRange.StartsWith(">=") || versionRange.StartsWith("<=") ||
            versionRange.StartsWith(">") || versionRange.StartsWith("<"))
        {
            var range = VersionManager.ParseVersionRange(versionRange);
            // 如果有最小版本，返回最小版本；否则返回最大版本
            if (!string.IsNullOrEmpty(range.MinVersion))
            {
                return range.MinVersion;
            }

            if (!string.IsNullOrEmpty(range.MaxVersion))
            {
                return range.MaxVersion;
            }

            return "1.0.0";
        }

        // 处理范围版本 (1.0.0-2.0.0)
        if (versionRange.Contains('-') && !versionRange.StartsWith("-"))
        {
            var parts = versionRange.Split('-', 2);
            if (parts.Length == 2)
            {
                var first = parts[0].Trim();
                var second = parts[1].Trim();

                // 检查是否是版本范围（两部分都是版本号）
                var firstParsed = VersionManager.ParseVersion(first);
                var secondParsed = VersionManager.ParseVersion(second);

                if (firstParsed.Major > 0 || firstParsed.Minor > 0 || firstParsed.Patch > 0)
                {
                    // 返回范围的最小版本
                    return firstParsed.ToString();
                }
            }
        }

        // 处理通配符版本 (1.2.*)
        if (versionRange.Contains("*"))
        {
            var range = VersionManager.ParseVersionRange(versionRange);
            return range.MinVersion;
        }

        // 精确版本 - 直接解析并返回
        var version = VersionManager.ParseVersion(versionRange);
        return version.ToString();
    }

    #region 打包功能

    /// <summary>
    /// 打包包文件夹为 .o8pkg 文件
    /// </summary>
    /// <param name="sourcePath">包文件夹路径</param>
    /// <param name="outputPath">输出文件路径（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的包文件路径</returns>
    public async Task<string> PackAsync(
        string sourcePath,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        return await ArchiveService.PackAsync(sourcePath, outputPath, cancellationToken);
    }

    /// <summary>
    /// 解包 .o8pkg 文件
    /// </summary>
    /// <param name="packagePath">包文件路径</param>
    /// <param name="destinationPath">目标文件夹路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UnpackAsync(
        string packagePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        await ArchiveService.UnpackAsync(packagePath, destinationPath, cancellationToken);
    }

    /// <summary>
    /// 验证包文件夹结构
    /// </summary>
    /// <param name="sourcePath">包文件夹路径</param>
    /// <returns>验证结果（是否有效，错误消息）</returns>
    public async Task<(bool IsValid, string Message)> ValidatePackageStructureAsync(string sourcePath)
    {
        return await ArchiveService.ValidatePackageStructureAsync(sourcePath);
    }

    /// <summary>
    /// 从包文件夹读取包元数据
    /// </summary>
    /// <param name="sourcePath">包文件夹路径</param>
    /// <returns>包元数据</returns>
    public async Task<Package?> ReadPackageMetadataAsync(string sourcePath)
    {
        return await ArchiveService.ReadPackageMetadataAsync(sourcePath);
    }

    #endregion

    #region 签名功能

    /// <summary>
    /// 签名包文件
    /// </summary>
    /// <param name="packagePath">包文件路径</param>
    /// <param name="certificate">用于签名的证书</param>
    /// <returns>包签名信息</returns>
    public async Task<PackageSignature> SignPackageAsync(
        string packagePath,
        X509Certificate2 certificate)
    {
        return await SignatureService.SignPackageAsync(packagePath, certificate);
    }

    /// <summary>
    /// 验证包签名
    /// </summary>
    /// <param name="packagePath">包文件路径</param>
    /// <param name="signature">签名信息</param>
    /// <returns>签名是否有效</returns>
    public async Task<bool> VerifySignatureAsync(
        string packagePath,
        PackageSignature signature)
    {
        return await SignatureService.VerifySignatureAsync(packagePath, signature);
    }

    /// <summary>
    /// 从文件读取签名
    /// </summary>
    /// <param name="signatureFilePath">签名文件路径</param>
    /// <returns>包签名信息</returns>
    public async Task<PackageSignature?> ReadSignatureAsync(string signatureFilePath)
    {
        return await SignatureService.ReadSignatureAsync(signatureFilePath);
    }

    /// <summary>
    /// 将签名写入文件
    /// </summary>
    /// <param name="signature">签名信息</param>
    /// <param name="signatureFilePath">签名文件路径</param>
    public async Task WriteSignatureAsync(
        PackageSignature signature,
        string signatureFilePath)
    {
        await SignatureService.WriteSignatureAsync(signature, signatureFilePath);
    }

    /// <summary>
    /// 生成自签名证书
    /// </summary>
    /// <param name="subjectName">证书主题名称</param>
    /// <param name="email">电子邮件（可选）</param>
    /// <param name="validityYears">有效期（年）</param>
    /// <returns>生成的证书</returns>
    public X509Certificate2 GenerateSelfSignedCertificate(
        string subjectName,
        string? email = null,
        int validityYears = 5)
    {
        return SignatureService.GenerateSelfSignedCertificate(subjectName, email, validityYears);
    }

    /// <summary>
    /// 从文件加载证书
    /// </summary>
    /// <param name="certPath">证书文件路径</param>
    /// <param name="password">证书密码（可选）</param>
    /// <returns>证书</returns>
    public async Task<X509Certificate2> LoadCertificateAsync(
        string certPath,
        string? password = null)
    {
        return await SignatureService.LoadCertificateAsync(certPath, password);
    }

    /// <summary>
    /// 导出证书到文件
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="password">密码（可选）</param>
    public async Task ExportCertificateAsync(
        X509Certificate2 certificate,
        string outputPath,
        string? password = null)
    {
        await SignatureService.ExportCertificateAsync(certificate, outputPath, password);
    }

    /// <summary>
    /// 获取证书信息
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <returns>证书信息字符串</returns>
    public string GetCertificateInfo(X509Certificate2 certificate)
    {
        return SignatureService.GetCertificateInfo(certificate);
    }

    #endregion
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