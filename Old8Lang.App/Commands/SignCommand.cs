using Old8Lang.App.Services;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang sign - 包签名命令
/// </summary>
public class SignCommand : ICommand
{
    public string Name => "sign";
    public string Description => "对包文件进行数字签名";

    public string Help => @"
用法: old8lang sign [选项] <包文件>

参数:
  <包文件>               .o8pkg 包文件路径（必需）

选项:
  -c, --cert <路径>      证书文件路径（.pfx 或 .cer）
  -p, --password <密码>  证书密码（用于加密的 .pfx 文件）
  -o, --output <路径>    签名文件输出路径（默认为 <包文件>.sig）
  --auto-cert            自动生成自签名证书
  --cert-name <名称>     自动生成证书时的主题名称
  --cert-email <邮箱>    自动生成证书时的电子邮件
  -h, --help             显示帮助信息

示例:
  # 使用现有证书签名
  old8lang sign package.o8pkg -c my-cert.pfx -p mypassword

  # 自动生成证书并签名
  old8lang sign package.o8pkg --auto-cert --cert-name ""John Doe"" --cert-email john@example.com

  # 指定签名文件输出路径
  old8lang sign package.o8pkg -c my-cert.pfx -o custom.sig
";

    public int Execute(string[] args)
    {
        // 解析参数
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        // 获取包文件路径
        var packagePath = GetPackagePath(args);
        if (string.IsNullOrEmpty(packagePath))
        {
            CommandHelper.PrintError("错误: 缺少包文件路径参数");
            Console.WriteLine("使用 old8lang sign -h 查看帮助");
            return 1;
        }

        // 转换为绝对路径
        packagePath = Path.GetFullPath(packagePath);

        if (!File.Exists(packagePath))
        {
            CommandHelper.PrintError($"错误: 包文件不存在: {packagePath}");
            return 1;
        }

        if (!packagePath.EndsWith(".o8pkg", StringComparison.OrdinalIgnoreCase))
        {
            CommandHelper.PrintError("错误: 无效的包文件扩展名（应为 .o8pkg）");
            return 1;
        }

        try
        {
            var service = new PackageService(Directory.GetCurrentDirectory());

            // 获取或生成证书
            var certificate = GetOrGenerateCertificate(service, args);
            if (certificate == null)
            {
                return 1;
            }

            // 显示证书信息
            CommandHelper.PrintInfo("\n证书信息:");
            Console.WriteLine(service.GetCertificateInfo(certificate));

            // 检查证书是否过期
            if (certificate.NotAfter < DateTime.UtcNow)
            {
                CommandHelper.PrintWarning("\n⚠ 警告: 证书已过期！");
                Console.Write("是否继续签名？(y/N): ");
                var response = Console.ReadLine()?.Trim().ToLower();
                if (response != "y" && response != "yes")
                {
                    CommandHelper.PrintInfo("已取消签名操作");
                    return 0;
                }
            }

            // 签名包
            CommandHelper.PrintInfo($"\n正在签名 {Path.GetFileName(packagePath)}...");
            var signature = service.SignPackageAsync(packagePath, certificate).GetAwaiter().GetResult();

            // 保存签名文件
            var signaturePath = GetSignaturePath(args, packagePath);
            service.WriteSignatureAsync(signature, signaturePath).GetAwaiter().GetResult();

            // 显示结果
            CommandHelper.PrintSuccess("\n✓ 签名完成!");
            Console.WriteLine($"签名文件: {signaturePath}");
            Console.WriteLine($"\n签名信息:");
            Console.WriteLine($"  算法: {signature.Algorithm}");
            Console.WriteLine($"  签名者: {signature.Signer.Name}");
            Console.WriteLine($"  邮箱: {signature.Signer.Email}");
            Console.WriteLine($"  签名时间: {signature.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"  证书指纹: {signature.Signer.CertificateThumbprint}");

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"签名失败: {ex.Message}");
#if DEBUG
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
#endif
            return 1;
        }
    }

    private System.Security.Cryptography.X509Certificates.X509Certificate2? GetOrGenerateCertificate(
        PackageService service, string[] args)
    {
        var autoCert = args.Contains("--auto-cert");
        var certPath = GetOptionValue(args, "-c", "--cert");

        // 自动生成证书
        if (autoCert)
        {
            CommandHelper.PrintInfo("正在生成自签名证书...");

            var certName = GetOptionValue(args, "--cert-name") ?? "Old8Lang Package Publisher";
            var certEmail = GetOptionValue(args, "--cert-email");

            var certificate = service.GenerateSelfSignedCertificate(certName, certEmail, 5);

            // 询问是否保存证书
            Console.Write("\n是否保存生成的证书？(Y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();
            if (response != "n" && response != "no")
            {
                Console.Write("保存路径（默认 publisher.pfx）: ");
                var savePath = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = "publisher.pfx";
                }

                Console.Write("证书密码（按回车跳过）: ");
                var password = ReadPassword();

                service.ExportCertificateAsync(certificate, savePath, password).GetAwaiter().GetResult();
                CommandHelper.PrintSuccess($"✓ 证书已保存到: {savePath}");
            }

            return certificate;
        }

        // 从文件加载证书
        if (!string.IsNullOrEmpty(certPath))
        {
            if (!File.Exists(certPath))
            {
                CommandHelper.PrintError($"错误: 证书文件不存在: {certPath}");
                return null;
            }

            var password = GetOptionValue(args, "-p", "--password");

            // 如果没有提供密码，询问用户
            if (string.IsNullOrEmpty(password) && certPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("请输入证书密码（按回车跳过）: ");
                password = ReadPassword();
            }

            CommandHelper.PrintInfo($"正在加载证书: {certPath}");
            return service.LoadCertificateAsync(certPath, password).GetAwaiter().GetResult();
        }

        // 没有指定证书
        CommandHelper.PrintError("错误: 必须指定证书文件（-c）或使用自动生成（--auto-cert）");
        Console.WriteLine("使用 old8lang sign -h 查看帮助");
        return null;
    }

    private string? GetPackagePath(string[] args)
    {
        // 查找第一个不是选项的参数
        foreach (var arg in args)
        {
            if (!arg.StartsWith("-"))
            {
                return arg;
            }
        }

        return null;
    }

    private string GetSignaturePath(string[] args, string packagePath)
    {
        var outputPath = GetOptionValue(args, "-o", "--output");
        if (!string.IsNullOrEmpty(outputPath))
        {
            return Path.GetFullPath(outputPath);
        }

        // 默认：包文件路径 + .sig
        return packagePath + ".sig";
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
}
