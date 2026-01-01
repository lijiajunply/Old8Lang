using Old8Lang.App.Services;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang unpack - 解包命令
/// </summary>
public class UnpackCommand : ICommand
{
    public string Name => "unpack";
    public string Description => "解包 .o8pkg 文件到指定目录";

    public string Help => @"
用法: old8lang unpack [选项] <包文件>

参数:
  <包文件>               .o8pkg 包文件路径（必需）

选项:
  -o, --output <路径>    解包目标目录（可选，默认为当前目录下的包名文件夹）
  -f, --force            强制覆盖已存在的目录
  -h, --help             显示帮助信息

示例:
  old8lang unpack package.o8pkg                     # 解包到默认位置
  old8lang unpack package.o8pkg -o ./extracted      # 解包到指定目录
  old8lang unpack package.o8pkg -f                  # 强制覆盖已存在的目录
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
            Console.WriteLine("使用 old8lang unpack -h 查看帮助");
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

        // 获取输出路径
        var outputPath = GetOutputPath(args, packagePath);
        var force = args.Contains("-f") || args.Contains("--force");

        // 检查目标目录
        if (Directory.Exists(outputPath))
        {
            if (!force)
            {
                CommandHelper.PrintError($"错误: 目标目录已存在: {outputPath}");
                Console.WriteLine("使用 -f 或 --force 选项强制覆盖");
                return 1;
            }

            CommandHelper.PrintWarning($"警告: 将覆盖已存在的目录: {outputPath}");
        }

        try
        {
            var service = new PackageService(Directory.GetCurrentDirectory());

            // 解包
            CommandHelper.PrintInfo($"正在解包 {Path.GetFileName(packagePath)}...");
            service.UnpackAsync(packagePath, outputPath).GetAwaiter().GetResult();

            // 读取包信息
            var package = service.ReadPackageMetadataAsync(outputPath).GetAwaiter().GetResult();

            CommandHelper.PrintSuccess("\n✓ 解包完成!");
            Console.WriteLine($"输出目录: {outputPath}");

            if (package != null)
            {
                Console.WriteLine($"\n包信息:");
                Console.WriteLine($"  名称: {package.Id}");
                Console.WriteLine($"  版本: {package.Version}");
                Console.WriteLine($"  作者: {package.Author}");
                Console.WriteLine($"  描述: {package.Description}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"解包失败: {ex.Message}");
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

    private string GetOutputPath(string[] args, string packagePath)
    {
        // 检查是否指定了输出路径
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length)
            {
                return Path.GetFullPath(args[i + 1]);
            }
        }

        // 默认输出路径：当前目录下的包名文件夹
        var packageName = Path.GetFileNameWithoutExtension(packagePath);
        return Path.Combine(Directory.GetCurrentDirectory(), packageName);
    }
}
