using Old8Lang.App.Services;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang pack - 打包命令
/// </summary>
public class PackCommand : ICommand
{
    public string Name => "pack";
    public string Description => "将包文件夹打包成 .o8pkg 文件";

    public string Help => @"
用法: old8lang pack [选项] <源路径>

参数:
  <源路径>               包文件夹路径（必需）

选项:
  -o, --output <路径>    输出文件路径（可选，默认在源路径旁边生成）
  -v, --validate         仅验证包结构，不执行打包
  -h, --help             显示帮助信息

示例:
  old8lang pack ./my-package                        # 打包到默认位置
  old8lang pack ./my-package -o dist/package.o8pkg  # 指定输出路径
  old8lang pack ./my-package -v                     # 仅验证包结构
";

    public async Task<int> ExecuteAsync(string[] args)
    {
        // 解析参数
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        // 获取源路径
        var sourcePath = GetSourcePath(args);
        if (string.IsNullOrEmpty(sourcePath))
        {
            CommandHelper.PrintError("错误: 缺少源路径参数");
            Console.WriteLine("使用 old8lang pack -h 查看帮助");
            return 1;
        }

        // 转换为绝对路径
        sourcePath = Path.GetFullPath(sourcePath);

        if (!Directory.Exists(sourcePath))
        {
            CommandHelper.PrintError($"错误: 源路径不存在: {sourcePath}");
            return 1;
        }

        // 获取输出路径
        var outputPath = GetOutputPath(args);
        var validateOnly = args.Contains("-v") || args.Contains("--validate");

        try
        {
            var service = new PackageService(Directory.GetCurrentDirectory());

            // 1. 验证包结构
            CommandHelper.PrintInfo("正在验证包结构...");
            var (isValid, message) = await service.ValidatePackageStructureAsync(sourcePath);

            if (!isValid)
            {
                CommandHelper.PrintError($"✗ 包结构验证失败: {message}");
                return 1;
            }

            CommandHelper.PrintSuccess("✓ 包结构验证通过");

            // 2. 读取包信息
            var package = await service.ReadPackageMetadataAsync(sourcePath);
            if (package == null)
            {
                CommandHelper.PrintError("错误: 无法读取包元数据");
                return 1;
            }

            Console.WriteLine($"包名称: {package.Id}");
            Console.WriteLine($"包版本: {package.Version}");
            Console.WriteLine($"包作者: {package.Author}");
            Console.WriteLine($"包描述: {package.Description}");

            // 如果只是验证，到这里就结束
            if (validateOnly)
            {
                CommandHelper.PrintSuccess("\n✓ 包验证完成");
                return 0;
            }

            // 3. 执行打包
            CommandHelper.PrintInfo("\n正在打包...");
            var resultPath = await service.PackAsync(sourcePath, outputPath);

            // 4. 显示结果
            var fileInfo = new FileInfo(resultPath);
            CommandHelper.PrintSuccess($"\n✓ 打包完成!");
            Console.WriteLine($"输出文件: {resultPath}");
            Console.WriteLine($"文件大小: {FormatFileSize(fileInfo.Length)}");
            Console.WriteLine($"包校验和: {package.Checksum}");

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"打包失败: {ex.Message}");
#if DEBUG
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
#endif
            return 1;
        }
    }

    private string? GetSourcePath(string[] args)
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

    private string? GetOutputPath(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
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
