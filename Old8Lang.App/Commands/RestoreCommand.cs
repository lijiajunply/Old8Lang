using Old8Lang.App.Services;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang restore - 恢复所有依赖命令
/// </summary>
public class RestoreCommand : ICommand
{
    public string Name => "restore";
    public string Description => "恢复项目的所有依赖（基于 o8package.json）";

    public string Help => @"
用法: old8lang restore [选项]

说明:
  restore 命令会读取 o8package.json 中的依赖配置，
  从 Old8Lang.PackageManager.Server 自动解析依赖关系并安装所有必需的包。

选项:
  --production           只恢复生产依赖（跳过 devDependencies）
  -r, --registry <地址>  指定远程 Old8Lang.PackageManager.Server 地址
  -k, --api-key <密钥>   指定访问私有仓库的 API 密钥
  -h, --help             显示帮助信息

示例:
  old8lang restore                                    # 恢复所有依赖
  old8lang restore --production                       # 只恢复生产依赖
  old8lang restore --registry http://localhost:5000   # 从自建私有仓库恢复
";

    public int Execute(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var projectRoot = CommandHelper.FindProjectRoot();
        if (projectRoot == null)
        {
            CommandHelper.PrintError("当前目录不是 Old8Lang 项目");
            CommandHelper.PrintInfo("请先运行 'old8lang init' 初始化项目");
            return 1;
        }

        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            CommandHelper.PrintError("无法加载项目配置");
            return 1;
        }

        var productionOnly = args.Contains("--production");
        var registryOverride = GetOptionValue(args, "-r", "--registry");
        var apiKeyOverride = GetOptionValue(args, "-k", "--api-key");

        using var packageService = new PackageService(projectRoot, config, registryOverride, apiKeyOverride);

        Console.WriteLine($"正在恢复 {config.ProjectName}@{config.Version} 的依赖...");
        Console.WriteLine($"包仓库: {packageService.RegistryUrl}");
        Console.WriteLine();

        var totalDeps = productionOnly
            ? config.References.Count(r => !r.IsDevDependency)
            : config.References.Count;

        if (totalDeps == 0)
        {
            CommandHelper.PrintInfo("没有需要恢复的依赖");
            return 0;
        }

        Console.WriteLine($"将恢复 {totalDeps} 个依赖包...\n");

        var result = packageService.RestorePackagesAsync(config, productionOnly).GetAwaiter().GetResult();

        Console.WriteLine();
        Console.WriteLine("========================================");

        if (result.Success)
        {
            CommandHelper.PrintSuccess($"恢复完成: {result.InstalledCount} 个包已安装");
        }
        else
        {
            CommandHelper.PrintError("恢复失败");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                CommandHelper.PrintError($"错误: {result.ErrorMessage}");
            }
        }

        if (result.SkippedCount > 0)
        {
            CommandHelper.PrintInfo($"跳过: {result.SkippedCount} 个包（已存在）");
        }

        if (result.FailedCount > 0)
        {
            CommandHelper.PrintError($"失败: {result.FailedCount} 个包");
            if (result.FailedPackages.Count > 0)
            {
                Console.WriteLine("\n失败的包:");
                foreach (var pkg in result.FailedPackages)
                {
                    Console.WriteLine($"  - {pkg}");
                }
            }
        }

        Console.WriteLine("========================================");

        return result.Success ? 0 : 1;
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
}
