using Old8Lang.App.Services;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang restore - 恢复所有依赖命令
/// </summary>
public class RestoreCommand : ICommand
{
    public string Name => "restore";
    public string Description => "恢复项目的所有依赖（基于 o8packages.json）";

    public string Help => @"
用法: old8lang restore [选项]

说明:
  restore 命令会读取 o8packages.json 中的依赖配置，
  自动解析依赖关系并安装所有必需的包。

  这个命令集成了 Old8Lang.PackageManager.Core 库，
  支持依赖解析、版本管理和冲突处理。

选项:
  --production           只恢复生产依赖（跳过 devDependencies）
  --frozen-lockfile      使用精确的锁文件版本，不更新
  -h, --help             显示帮助信息

示例:
  old8lang restore                   # 恢复所有依赖
  old8lang restore --production      # 只恢复生产依赖
  old8lang restore --frozen-lockfile # 使用锁文件精确版本
";

    public async Task<int> ExecuteAsync(string[] args)
    {
        // 检查帮助
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        // 检查项目配置
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
        var frozenLockfile = args.Contains("--frozen-lockfile");

        Console.WriteLine($"正在恢复 {config.Name}@{config.Version} 的依赖...");
        Console.WriteLine();

        // 创建包服务
        var packageService = new PackageService(projectRoot, config);

        // 加载锁文件提示
        if (frozenLockfile)
        {
            var lockFilePath = Path.Combine(projectRoot, LockFile.FileName);
            if (File.Exists(lockFilePath))
            {
                CommandHelper.PrintInfo("使用锁文件中的精确版本");
            }
            else
            {
                CommandHelper.PrintWarning("锁文件不存在，将使用 o8packages.json 中的版本范围");
            }
        }

        // 显示要安装的包
        var totalDeps = config.Dependencies.Count +
                       (productionOnly ? 0 : config.DevDependencies.Count);

        if (totalDeps == 0)
        {
            CommandHelper.PrintInfo("没有需要恢复的依赖");
            return 0;
        }

        Console.WriteLine($"将恢复 {totalDeps} 个依赖包...\n");

        // 恢复依赖
        var result = await packageService.RestorePackagesAsync(config, productionOnly, frozenLockfile);

        // 显示结果
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

        // 更新锁文件
        if (result.Success && !frozenLockfile)
        {
            try
            {
                var lockFile = LockFile.Generate(config, packageService.PackagesDirectory);
                lockFile.SaveToDirectory(projectRoot);
                CommandHelper.PrintInfo("已更新锁文件");
            }
            catch (Exception ex)
            {
                CommandHelper.PrintWarning($"更新锁文件失败: {ex.Message}");
            }
        }

        Console.WriteLine("========================================");

        return result.Success ? 0 : 1;
    }
}
