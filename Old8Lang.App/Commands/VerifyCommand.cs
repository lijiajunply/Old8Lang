using Old8Lang.App.Services;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang verify - 验证包签名命令
/// </summary>
public class VerifyCommand : ICommand
{
    public string Name => "verify";
    public string Description => "验证包文件的数字签名";

    public string Help => @"
用法: old8lang verify [选项] <包文件>

参数:
  <包文件>               .o8pkg 包文件路径（必需）

选项:
  -s, --signature <路径> 签名文件路径（默认为 <包文件>.sig）
  -v, --verbose          显示详细信息
  -h, --help             显示帮助信息

示例:
  old8lang verify package.o8pkg                     # 使用默认签名文件
  old8lang verify package.o8pkg -s custom.sig       # 指定签名文件
  old8lang verify package.o8pkg -v                  # 显示详细信息
";

    public async Task<int> ExecuteAsync(string[] args)
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
            Console.WriteLine("使用 old8lang verify -h 查看帮助");
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

        // 获取签名文件路径
        var signaturePath = GetSignaturePath(args, packagePath);
        var verbose = args.Contains("-v") || args.Contains("--verbose");

        try
        {
            var service = new PackageService(Directory.GetCurrentDirectory());

            // 检查签名文件是否存在
            if (!File.Exists(signaturePath))
            {
                CommandHelper.PrintError($"✗ 签名文件不存在: {signaturePath}");
                CommandHelper.PrintWarning("\n⚠ 此包未签名或签名文件丢失");
                return 1;
            }

            CommandHelper.PrintInfo($"正在验证 {Path.GetFileName(packagePath)} 的签名...");

            // 读取签名
            var signature = await service.ReadSignatureAsync(signaturePath);
            if (signature == null)
            {
                CommandHelper.PrintError("✗ 无法读取签名文件");
                return 1;
            }

            // 显示签名信息
            if (verbose)
            {
                Console.WriteLine("\n签名信息:");
                Console.WriteLine($"  算法: {signature.Algorithm}");
                Console.WriteLine($"  哈希算法: {signature.HashAlgorithm}");
                Console.WriteLine($"  签名者: {signature.Signer.Name}");
                Console.WriteLine($"  邮箱: {signature.Signer.Email}");
                Console.WriteLine($"  签名时间: {signature.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"  证书指纹: {signature.Signer.CertificateThumbprint}");
                Console.WriteLine($"  证书有效期: {signature.Signer.NotBefore:yyyy-MM-dd} 至 {signature.Signer.NotAfter:yyyy-MM-dd}");
            }

            // 检查证书有效期
            var now = DateTime.UtcNow;
            if (signature.Signer.NotAfter < now)
            {
                CommandHelper.PrintWarning($"\n⚠ 警告: 签名证书已过期（{signature.Signer.NotAfter:yyyy-MM-dd}）");
            }
            else if (signature.Signer.NotBefore > now)
            {
                CommandHelper.PrintWarning($"\n⚠ 警告: 签名证书尚未生效（{signature.Signer.NotBefore:yyyy-MM-dd}）");
            }

            // 验证签名
            Console.WriteLine("\n正在验证签名...");
            bool isValid = await service.VerifySignatureAsync(packagePath, signature);

            if (isValid)
            {
                // 签名验证通过
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ 签名验证通过");
                Console.ResetColor();

                if (!verbose)
                {
                    Console.WriteLine($"签名者: {signature.Signer.Name}");
                    Console.WriteLine($"签名时间: {signature.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                }

                Console.WriteLine("\n此包的完整性已验证，未被篡改。");
                return 0;
            }
            else
            {
                // 签名验证失败
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n✗ 签名验证失败");
                Console.ResetColor();

                Console.WriteLine("\n可能的原因:");
                Console.WriteLine("  1. 包文件已被篡改");
                Console.WriteLine("  2. 签名文件损坏");
                Console.WriteLine("  3. 签名与包文件不匹配");

                CommandHelper.PrintWarning("\n⚠ 警告: 此包可能不安全，请勿安装或使用！");
                return 1;
            }
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"验证失败: {ex.Message}");
#if DEBUG
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
#endif
            return 1;
        }
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
        // 检查是否指定了签名文件路径
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-s" || args[i] == "--signature") && i + 1 < args.Length)
            {
                return Path.GetFullPath(args[i + 1]);
            }
        }

        // 默认：包文件路径 + .sig
        return packagePath + ".sig";
    }
}
