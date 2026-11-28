using Old8Lang.App.Services;
using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang publish - 打包、签名并推送到 Old8Lang.PackageManager.Server
/// </summary>
public class PublishCommand : ICommand
{
    public string Name => "publish";
    public string Description => "打包、签名并发布包到 Old8Lang 包仓库";

    public string Help => @"
用法: old8lang publish [选项]

选项:
  -o, --output <目录>    本地产物输出目录（默认 ./dist）
  -r, --registry <地址>  指定目标 Old8Lang.PackageManager.Server 地址（覆盖默认/项目配置）
  -k, --api-key <密钥>   指定发布用 API 密钥（也可通过 OLD8LANG_API_KEY 或 registry set-key 设置）
  --local-only           仅本地打包和签名，不上传到远程包仓库
  --prerelease           标记为预发布版本
  -f, --force            覆盖已有本地产物时不提示确认
  -c, --cert <路径>      证书文件路径
  -p, --password <密码>  证书密码
  --auto-cert            自动生成自签名证书（仅用于开发/测试）
  --cert-name <名称>     自动生成证书时的主题名称
  --cert-email <邮箱>    自动生成证书时的电子邮件
  --no-sign              跳过签名步骤
  --skip-validation      跳过发布前验证
  -h, --help             显示帮助信息

发布流程:
  1. 读取项目配置（o8package.json）
  2. 验证包结构和元数据
  3. 打包为 .o8pkg 文件
  4. 签名包文件（可选）
  5. 上传到远程 Old8Lang.PackageManager.Server（除非指定 --local-only）

示例:
  # 打包、签名并推送到当前配置的包仓库
  old8lang publish -c my-cert.pfx -p mypassword

  # 推送到自建私有包仓库（不签名）
  old8lang publish --no-sign --registry http://localhost:5000 --api-key <KEY>

  # 仅本地打包签名，不上传
  old8lang publish --local-only --auto-cert --cert-name ""Dev Publisher""
";

    public int Execute(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var outputDir = GetOptionValue(args, "-o", "--output") ?? "dist";
        var registryOverride = GetOptionValue(args, "-r", "--registry");
        var apiKeyOverride = GetOptionValue(args, "-k", "--api-key");
        var localOnly = args.Contains("--local-only");
        var isPrerelease = args.Contains("--prerelease");
        var force = args.Contains("-f") || args.Contains("--force");
        var certPath = GetOptionValue(args, "-c", "--cert");
        var password = GetOptionValue(args, "-p", "--password");
        var autoCert = args.Contains("--auto-cert");
        var certName = GetOptionValue(args, "--cert-name");
        var certEmail = GetOptionValue(args, "--cert-email");
        var noSign = args.Contains("--no-sign");
        var skipValidation = args.Contains("--skip-validation");

        var totalSteps = localOnly ? 5 : 6;

        try
        {
            var projectRoot = CommandHelper.FindProjectRoot();
            if (projectRoot == null)
            {
                CommandHelper.PrintError("错误: 未找到 Old8Lang 项目（o8package.json）");
                CommandHelper.PrintInfo("请在项目根目录下运行此命令，或先运行 'old8lang init' 初始化项目");
                return 1;
            }

            Console.WriteLine("========================================");
            Console.WriteLine("  Old8Lang 包发布");
            Console.WriteLine("========================================\n");

            CommandHelper.PrintInfo($"项目根目录: {projectRoot}");

            // 1. 读取项目配置
            CommandHelper.PrintInfo($"\n[步骤 1/{totalSteps}] 读取项目配置...");
            var projectConfig = ProjectConfig.LoadFromDirectory(projectRoot);
            if (projectConfig == null)
            {
                CommandHelper.PrintError("✗ 无法读取项目配置");
                return 1;
            }

            using var service = new PackageService(projectRoot, projectConfig, registryOverride, apiKeyOverride);

            Console.WriteLine($"  项目名称: {projectConfig.ProjectName}");
            Console.WriteLine($"  项目版本: {projectConfig.Version}");
            Console.WriteLine($"  作者: {projectConfig.Author}");
            if (!localOnly)
            {
                Console.WriteLine($"  目标仓库: {service.RegistryUrl}");
            }
            CommandHelper.PrintSuccess("✓ 配置读取完成");

            // 2. 验证包结构
            if (!skipValidation)
            {
                CommandHelper.PrintInfo($"\n[步骤 2/{totalSteps}] 验证包结构...");
                var (isValid, message) = service.ValidatePackageStructureAsync(projectRoot).GetAwaiter().GetResult();

                if (!isValid)
                {
                    CommandHelper.PrintError($"✗ 包结构验证失败: {message}");
                    CommandHelper.PrintInfo("使用 --skip-validation 跳过验证（不推荐）");
                    return 1;
                }
                CommandHelper.PrintSuccess("✓ 包结构验证通过");
            }
            else
            {
                CommandHelper.PrintWarning($"\n[步骤 2/{totalSteps}] 跳过包结构验证");
            }

            // 3. 准备输出目录
            CommandHelper.PrintInfo($"\n[步骤 3/{totalSteps}] 准备输出目录...");
            var fullOutputDir = Path.GetFullPath(outputDir);
            if (!Directory.Exists(fullOutputDir))
            {
                Directory.CreateDirectory(fullOutputDir);
            }
            Console.WriteLine($"  输出目录: {fullOutputDir}");
            CommandHelper.PrintSuccess("✓ 输出目录准备完成");

            // 4. 打包
            CommandHelper.PrintInfo($"\n[步骤 4/{totalSteps}] 打包项目...");
            var packageFileName = $"{projectConfig.ProjectName}.{projectConfig.Version}.o8pkg";
            var packagePath = Path.Combine(fullOutputDir, packageFileName);

            if (File.Exists(packagePath))
            {
                if (!force)
                {
                    Console.Write($"  包文件已存在: {packageFileName}\n  是否覆盖？(y/N): ");
                    var response = Console.ReadLine()?.Trim().ToLower();
                    if (response != "y" && response != "yes")
                    {
                        CommandHelper.PrintInfo("已取消发布");
                        return 0;
                    }
                }
                File.Delete(packagePath);
            }

            service.PackAsync(projectRoot, packagePath).GetAwaiter().GetResult();
            var packageInfo = new FileInfo(packagePath);
            Console.WriteLine($"  包文件: {packageFileName}");
            Console.WriteLine($"  文件大小: {FormatFileSize(packageInfo.Length)}");
            CommandHelper.PrintSuccess("✓ 打包完成");

            // 5. 签名
            string? signaturePath = null;
            if (noSign)
            {
                CommandHelper.PrintWarning($"\n[步骤 5/{totalSteps}] 跳过签名（不推荐用于生产环境）");
            }
            else
            {
                CommandHelper.PrintInfo($"\n[步骤 5/{totalSteps}] 签名包文件...");

                var certificate = GetOrGenerateCertificate(
                    service,
                    autoCert,
                    certPath,
                    password,
                    certName,
                    certEmail,
                    projectConfig
                );

                if (certificate == null)
                {
                    return 1;
                }

                if (certificate.NotAfter < DateTime.UtcNow)
                {
                    CommandHelper.PrintWarning("⚠ 警告: 证书已过期！");
                    Console.Write("是否继续签名？(y/N): ");
                    var response = Console.ReadLine()?.Trim().ToLower();
                    if (response != "y" && response != "yes")
                    {
                        CommandHelper.PrintInfo("已取消签名");
                        return 0;
                    }
                }

                var signature = service.SignPackageAsync(packagePath, certificate).GetAwaiter().GetResult();
                signaturePath = packagePath + ".sig";
                service.WriteSignatureAsync(signature, signaturePath).GetAwaiter().GetResult();

                Console.WriteLine($"  签名者: {signature.Signer.Name}");
                Console.WriteLine($"  签名时间: {signature.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"  签名文件: {Path.GetFileName(signaturePath)}");
                CommandHelper.PrintSuccess("✓ 签名完成");
            }

            // 6. 上传到远程 Old8Lang.PackageManager.Server
            if (!localOnly)
            {
                CommandHelper.PrintInfo($"\n[步骤 6/{totalSteps}] 上传包到 {service.RegistryUrl}...");
                var publishOptions = new PackagePublishOptions
                {
                    Language = "old8lang",
                    Author = projectConfig.Author,
                    Description = projectConfig.Description,
                    License = projectConfig.License,
                    ProjectUrl = projectConfig.ProjectUrl,
                    Tags = projectConfig.Tags,
                    IsPrerelease = isPrerelease || projectConfig.Version.Contains('-'),
                    SignatureFilePath = signaturePath
                };

                var publishResult = service.PublishPackageAsync(packagePath, publishOptions).GetAwaiter().GetResult();
                if (!publishResult.Success)
                {
                    CommandHelper.PrintError($"✗ 上传失败 (HTTP {publishResult.StatusCode}): {publishResult.Message}");
                    if (publishResult.StatusCode is 401 or 403)
                    {
                        CommandHelper.PrintInfo("提示: 请检查 API Key 配置（old8lang registry set-key <KEY> 或传入 --api-key <KEY>）");
                    }
                    return 1;
                }

                Console.WriteLine($"  已发布包: {publishResult.PackageId}@{publishResult.Version}");
                if (!string.IsNullOrEmpty(publishResult.Checksum))
                {
                    Console.WriteLine($"  服务端校验和: {publishResult.Checksum}");
                }
                CommandHelper.PrintSuccess("✓ 上传发布完成");
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine(localOnly ? "  本地打包完成！" : "  发布完成！");
            Console.WriteLine("========================================\n");
            Console.WriteLine("产物文件:");
            Console.WriteLine($"  📦 {packagePath}");
            if (signaturePath != null)
            {
                Console.WriteLine($"  ✍️  {signaturePath}");
            }

            if (!localOnly)
            {
                Console.WriteLine($"\n其他项目现在可以通过以下命令安装:");
                Console.WriteLine($"  old8lang install {projectConfig.ProjectName}@{projectConfig.Version}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"\n发布失败: {ex.Message}");
#if DEBUG
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
#endif
            return 1;
        }
    }

    private System.Security.Cryptography.X509Certificates.X509Certificate2? GetOrGenerateCertificate(
        PackageService service,
        bool autoCert,
        string? certPath,
        string? password,
        string? certName,
        string? certEmail,
        ProjectConfig projectConfig)
    {
        if (autoCert)
        {
            CommandHelper.PrintInfo("  正在生成自签名证书...");

            var name = certName ?? projectConfig.Author ?? "Old8Lang Package Publisher";
            var email = certEmail;

            var certificate = service.GenerateSelfSignedCertificate(name, email, 5);
            Console.WriteLine($"  证书主题: {name}");
            CommandHelper.PrintSuccess("  ✓ 证书生成完成");

            Console.Write("\n  是否保存生成的证书以供将来使用？(Y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();
            if (response != "n" && response != "no")
            {
                var defaultPath = $"{projectConfig.ProjectName}.publisher.pfx";
                Console.Write($"  保存路径（默认 {defaultPath}）: ");
                var savePath = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = defaultPath;
                }

                Console.Write("  证书密码（按回车跳过）: ");
                var savePassword = ReadPassword();

                service.ExportCertificateAsync(certificate, savePath, savePassword).GetAwaiter().GetResult();
                CommandHelper.PrintSuccess($"  ✓ 证书已保存到: {savePath}");
                CommandHelper.PrintInfo($"  下次发布可使用: old8lang publish -c {savePath}");
            }

            return certificate;
        }

        if (!string.IsNullOrEmpty(certPath))
        {
            if (!File.Exists(certPath))
            {
                CommandHelper.PrintError($"  ✗ 证书文件不存在: {certPath}");
                return null;
            }

            if (string.IsNullOrEmpty(password) && certPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("  请输入证书密码（按回车跳过）: ");
                password = ReadPassword();
            }

            CommandHelper.PrintInfo($"  正在加载证书: {certPath}");
            var certificate = service.LoadCertificateAsync(certPath, password).GetAwaiter().GetResult();
            Console.WriteLine($"  证书主题: {certificate.Subject}");
            CommandHelper.PrintSuccess("  ✓ 证书加载完成");
            return certificate;
        }

        CommandHelper.PrintError("  ✗ 错误: 必须指定证书文件（-c）或使用自动生成（--auto-cert）");
        CommandHelper.PrintInfo("  或者使用 --no-sign 跳过签名（不推荐）");
        Console.WriteLine("\n  使用 old8lang publish -h 查看帮助");
        return null;
    }

    private static string? GetOptionValue(string[] args, params string[] optionNames)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (optionNames.Contains(args[i]) && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static string? ReadPassword()
    {
        var password = string.Empty;
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write("*");
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return string.IsNullOrEmpty(password) ? null : password;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
