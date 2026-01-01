using Old8Lang.App.Services;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang cert - 证书管理命令
/// </summary>
public class CertCommand : ICommand
{
    public string Name => "cert";
    public string Description => "证书管理工具";

    public string Help => @"
用法: old8lang cert <子命令> [选项]

子命令:
  generate               生成自签名证书
  info                   查看证书信息
  export                 导出证书

generate 子命令选项:
  -n, --name <名称>      证书主题名称（必需）
  -e, --email <邮箱>     电子邮件地址（可选）
  -y, --years <年数>     有效期（年，默认 5 年）
  -o, --output <路径>    输出文件路径（默认 certificate.pfx）
  -p, --password <密码>  证书密码（用于 .pfx 文件）

info 子命令选项:
  -c, --cert <路径>      证书文件路径（必需）
  -p, --password <密码>  证书密码（用于加密的 .pfx 文件）

export 子命令选项:
  -c, --cert <路径>      输入证书文件路径（必需）
  -o, --output <路径>    输出文件路径（必需）
  -p, --password <密码>  输入证书密码
  --out-password <密码>  输出证书密码（仅适用于 .pfx）

通用选项:
  -h, --help             显示帮助信息

示例:
  # 生成自签名证书
  old8lang cert generate -n ""John Doe"" -e john@example.com -o my-cert.pfx -p mypassword

  # 查看证书信息
  old8lang cert info -c my-cert.pfx -p mypassword

  # 导出证书为公钥格式（.cer）
  old8lang cert export -c my-cert.pfx -p mypassword -o public-cert.cer
";

    public int Execute(string[] args)
    {
        // 解析参数
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var subCommand = args[0].ToLower();
        var subArgs = args.Skip(1).ToArray();

        return subCommand switch
        {
            "generate" or "gen" => ExecuteGenerateAsync(subArgs),
            "info" => ExecuteInfoAsync(subArgs),
            "export" => ExecuteExportAsync(subArgs),
            _ => HandleUnknownSubCommand(subCommand)
        };
    }

    private int ExecuteGenerateAsync(string[] args)
    {
        try
        {
            var name = GetOptionValue(args, "-n", "--name");
            if (string.IsNullOrEmpty(name))
            {
                CommandHelper.PrintError("错误: 必须指定证书名称（-n 或 --name）");
                return 1;
            }

            var email = GetOptionValue(args, "-e", "--email");
            var yearsStr = GetOptionValue(args, "-y", "--years") ?? "5";
            var outputPath = GetOptionValue(args, "-o", "--output") ?? "certificate.pfx";
            var password = GetOptionValue(args, "-p", "--password");

            if (!int.TryParse(yearsStr, out int years) || years < 1 || years > 30)
            {
                CommandHelper.PrintError("错误: 有效期必须是 1-30 之间的整数");
                return 1;
            }

            // 如果没有提供密码，询问用户
            if (string.IsNullOrEmpty(password) && outputPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("请输入证书密码（按回车跳过）: ");
                password = ReadPassword();
            }

            var service = new PackageService(Directory.GetCurrentDirectory());

            CommandHelper.PrintInfo("正在生成自签名证书...");
            var certificate = service.GenerateSelfSignedCertificate(name, email, years);

            CommandHelper.PrintInfo($"正在保存证书到: {outputPath}");
            service.ExportCertificateAsync(certificate, outputPath, password).GetAwaiter().GetResult();

            CommandHelper.PrintSuccess("\n✓ 证书生成成功!");
            Console.WriteLine($"证书文件: {Path.GetFullPath(outputPath)}");
            Console.WriteLine("\n证书信息:");
            Console.WriteLine(service.GetCertificateInfo(certificate));

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"生成证书失败: {ex.Message}");
#if DEBUG
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
#endif
            return 1;
        }
    }

    private int ExecuteInfoAsync(string[] args)
    {
        try
        {
            var certPath = GetOptionValue(args, "-c", "--cert");
            if (string.IsNullOrEmpty(certPath))
            {
                CommandHelper.PrintError("错误: 必须指定证书文件路径（-c 或 --cert）");
                return 1;
            }

            if (!File.Exists(certPath))
            {
                CommandHelper.PrintError($"错误: 证书文件不存在: {certPath}");
                return 1;
            }

            var password = GetOptionValue(args, "-p", "--password");

            // 如果没有提供密码且是 .pfx 文件，询问用户
            if (string.IsNullOrEmpty(password) && certPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("请输入证书密码（按回车跳过）: ");
                password = ReadPassword();
            }

            var service = new PackageService(Directory.GetCurrentDirectory());

            CommandHelper.PrintInfo($"正在读取证书: {certPath}");
            var certificate = service.LoadCertificateAsync(certPath, password).GetAwaiter().GetResult();

            Console.WriteLine("\n" + service.GetCertificateInfo(certificate));

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"读取证书失败: {ex.Message}");
#if DEBUG
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
#endif
            return 1;
        }
    }

    private int ExecuteExportAsync(string[] args)
    {
        try
        {
            var certPath = GetOptionValue(args, "-c", "--cert");
            if (string.IsNullOrEmpty(certPath))
            {
                CommandHelper.PrintError("错误: 必须指定输入证书文件路径（-c 或 --cert）");
                return 1;
            }

            var outputPath = GetOptionValue(args, "-o", "--output");
            if (string.IsNullOrEmpty(outputPath))
            {
                CommandHelper.PrintError("错误: 必须指定输出文件路径（-o 或 --output）");
                return 1;
            }

            if (!File.Exists(certPath))
            {
                CommandHelper.PrintError($"错误: 证书文件不存在: {certPath}");
                return 1;
            }

            var password = GetOptionValue(args, "-p", "--password");
            var outPassword = GetOptionValue(args, "--out-password");

            // 如果没有提供密码且是 .pfx 文件，询问用户
            if (string.IsNullOrEmpty(password) && certPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("请输入输入证书密码（按回车跳过）: ");
                password = ReadPassword();
            }

            if (string.IsNullOrEmpty(outPassword) && outputPath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("请输入输出证书密码（按回车跳过）: ");
                outPassword = ReadPassword();
            }

            var service = new PackageService(Directory.GetCurrentDirectory());

            CommandHelper.PrintInfo($"正在加载证书: {certPath}");
            var certificate = service.LoadCertificateAsync(certPath, password).GetAwaiter().GetResult();

            CommandHelper.PrintInfo($"正在导出证书到: {outputPath}");
            service.ExportCertificateAsync(certificate, outputPath, outPassword).GetAwaiter().GetResult();

            CommandHelper.PrintSuccess("\n✓ 证书导出成功!");
            Console.WriteLine($"输出文件: {Path.GetFullPath(outputPath)}");

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"导出证书失败: {ex.Message}");
#if DEBUG
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
#endif
            return 1;
        }
    }

    private int HandleUnknownSubCommand(string subCommand)
    {
        CommandHelper.PrintError($"错误: 未知子命令 '{subCommand}'");
        Console.WriteLine("使用 old8lang cert -h 查看帮助");
        return 1;
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
