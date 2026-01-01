using Old8Lang.App.Services;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang publish - 发布命令
/// 自动打包、签名并准备发布
/// </summary>
public class PublishCommand : ICommand
{
    public string Name => "publish";
    public string Description => "打包、签名并发布包";

    public string Help => @"
用法: old8lang publish [选项]

选项:
  -o, --output <目录>    输出目录（默认 ./dist）
  -c, --cert <路径>      证书文件路径
  -p, --password <密码>  证书密码
  --auto-cert            自动生成自签名证书（仅用于开发/测试）
  --cert-name <名称>     自动生成证书时的主题名称
  --cert-email <邮箱>    自动生成证书时的电子邮件
  --no-sign              跳过签名步骤
  --skip-validation      跳过发布前验证
  -h, --help             显示帮助信息

发布流程:
  1. 读取项目配置（o8packages.json）
  2. 验证包结构和元数据
  3. 打包为 .o8pkg 文件
  4. 签名包文件（可选）
  5. 输出发布文件到目标目录

示例:
  # 基本发布（使用现有证书）
  old8lang publish -c my-cert.pfx -p mypassword

  # 发布到指定目录
  old8lang publish -o ./release -c my-cert.pfx

  # 使用自动生成的证书（开发环境）
  old8lang publish --auto-cert --cert-name ""Dev Publisher""

  # 不签名发布（不推荐）
  old8lang publish --no-sign

注意:
  - 生产环境发布建议始终签名
  - 自动生成的证书仅适用于开发/测试
  - 发布前请确保项目配置正确
";

    public int Execute(string[] args)
    {
        // 解析参数
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var outputDir = GetOptionValue(args, "-o", "--output") ?? "dist";
        var certPath = GetOptionValue(args, "-c", "--cert");
        var password = GetOptionValue(args, "-p", "--password");
        var autoCert = args.Contains("--auto-cert");
        var certName = GetOptionValue(args, "--cert-name");
        var certEmail = GetOptionValue(args, "--cert-email");
        var noSign = args.Contains("--no-sign");
        var skipValidation = args.Contains("--skip-validation");

        try
        {
            // 1. 查找项目根目录
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

            // 2. 读取项目配置
            CommandHelper.PrintInfo("\n[步骤 1/5] 读取项目配置...");
            var projectConfig = ProjectConfig.LoadFromDirectory(projectRoot);
            if (projectConfig == null)
            {
                CommandHelper.PrintError("✗ 无法读取项目配置");
                return 1;
            }

            Console.WriteLine($"  项目名称: {projectConfig.ProjectName}");
            Console.WriteLine($"  项目版本: {projectConfig.Version}");
            Console.WriteLine($"  作者: {projectConfig.Author}");
            CommandHelper.PrintSuccess("✓ 配置读取完成");

            // 3. 验证包结构（如果没有跳过）
            if (!skipValidation)
            {
                CommandHelper.PrintInfo("\n[步骤 2/5] 验证包结构...");
                var service = new PackageService(projectRoot, projectConfig);
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
                CommandHelper.PrintWarning("\n[步骤 2/5] 跳过包结构验证");
            }

            // 4. 创建输出目录
            CommandHelper.PrintInfo("\n[步骤 3/5] 准备输出目录...");
            var fullOutputDir = Path.GetFullPath(outputDir);
            if (!Directory.Exists(fullOutputDir))
            {
                Directory.CreateDirectory(fullOutputDir);
            }
            Console.WriteLine($"  输出目录: {fullOutputDir}");
            CommandHelper.PrintSuccess("✓ 输出目录准备完成");

            // 5. 打包
            CommandHelper.PrintInfo("\n[步骤 4/5] 打包项目...");
            var service2 = new PackageService(projectRoot, projectConfig);
            var packageFileName = $"{projectConfig.ProjectName}.{projectConfig.Version}.o8pkg";
            var packagePath = Path.Combine(fullOutputDir, packageFileName);

            // 如果文件已存在，询问是否覆盖
            if (File.Exists(packagePath))
            {
                Console.Write($"  包文件已存在: {packageFileName}\n  是否覆盖？(y/N): ");
                var response = Console.ReadLine()?.Trim().ToLower();
                if (response != "y" && response != "yes")
                {
                    CommandHelper.PrintInfo("已取消发布");
                    return 0;
                }
                File.Delete(packagePath);
            }

            service2.PackAsync(projectRoot, packagePath).GetAwaiter().GetResult();
            var packageInfo = new FileInfo(packagePath);
            Console.WriteLine($"  包文件: {packageFileName}");
            Console.WriteLine($"  文件大小: {FormatFileSize(packageInfo.Length)}");
            CommandHelper.PrintSuccess("✓ 打包完成");

            // 6. 签名（如果需要）
            if (noSign)
            {
                CommandHelper.PrintWarning("\n[步骤 5/5] 跳过签名（不推荐用于生产环境）");
            }
            else
            {
                CommandHelper.PrintInfo("\n[步骤 5/5] 签名包文件...");

                // 获取或生成证书
                var certificate = GetOrGenerateCertificate(
                    service2,
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

                // 检查证书有效期
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

                // 签名
                var signature = service2.SignPackageAsync(packagePath, certificate).GetAwaiter().GetResult();
                var signaturePath = packagePath + ".sig";
                service2.WriteSignatureAsync(signature, signaturePath).GetAwaiter().GetResult();

                Console.WriteLine($"  签名者: {signature.Signer.Name}");
                Console.WriteLine($"  签名时间: {signature.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"  签名文件: {Path.GetFileName(signaturePath)}");
                CommandHelper.PrintSuccess("✓ 签名完成");
            }

            // 7. 显示发布摘要
            Console.WriteLine("\n========================================");
            Console.WriteLine("  发布完成！");
            Console.WriteLine("========================================\n");
            Console.WriteLine("发布文件:");
            Console.WriteLine($"  📦 {packagePath}");
            if (!noSign)
            {
                Console.WriteLine($"  ✍️  {packagePath}.sig");
            }

            Console.WriteLine("\n下一步:");
            Console.WriteLine("  1. 测试包: old8lang verify " + packageFileName);
            if (!noSign)
            {
                Console.WriteLine("  2. 分发包和签名文件到用户");
            }
            else
            {
                Console.WriteLine("  2. 分发包文件到用户（⚠️ 未签名）");
            }
            Console.WriteLine("  3. 或上传到包仓库");

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
        // 自动生成证书
        if (autoCert)
        {
            CommandHelper.PrintInfo("  正在生成自签名证书...");

            var name = certName ?? projectConfig.Author ?? "Old8Lang Package Publisher";
            var email = certEmail;

            var certificate = service.GenerateSelfSignedCertificate(name, email, 5);
            Console.WriteLine($"  证书主题: {name}");
            CommandHelper.PrintSuccess("  ✓ 证书生成完成");

            // 询问是否保存证书
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

        // 从文件加载证书
        if (!string.IsNullOrEmpty(certPath))
        {
            if (!File.Exists(certPath))
            {
                CommandHelper.PrintError($"  ✗ 证书文件不存在: {certPath}");
                return null;
            }

            // 如果没有提供密码，询问用户
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

        // 没有指定证书
        CommandHelper.PrintError("  ✗ 错误: 必须指定证书文件（-c）或使用自动生成（--auto-cert）");
        CommandHelper.PrintInfo("  或者使用 --no-sign 跳过签名（不推荐）");
        Console.WriteLine("\n  使用 old8lang publish -h 查看帮助");
        return null;
    }

    private string? GetOptionValue(string[] args, params string[] optionNames)
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

    private string? ReadPassword()
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
