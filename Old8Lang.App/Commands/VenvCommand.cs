using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang venv - 虚拟环境管理命令
/// </summary>
public class VenvCommand : ICommand
{
    public string Name => "venv";
    public string Description => "管理虚拟环境";

    public string Help => @"
用法: old8lang venv <子命令> [选项]

子命令:
  enable                 启用虚拟环境
  disable                禁用虚拟环境
  status                 查看虚拟环境状态
  clean                  清理虚拟环境（删除 packages 目录）
  -h, --help             显示帮助信息

示例:
  old8lang venv enable   # 启用虚拟环境
  old8lang venv disable  # 禁用虚拟环境
  old8lang venv status   # 查看状态
  old8lang venv clean    # 清理包目录
";

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            CommandHelper.PrintError("错误: 缺少子命令");
            Console.WriteLine(Help);
            return 1;
        }

        var subcommand = args[0].ToLower();

        return subcommand switch
        {
            "enable" => await EnableVenv(),
            "disable" => await DisableVenv(),
            "status" => await ShowStatus(),
            "clean" => await CleanVenv(),
            "-h" or "--help" => ShowHelpAndExit(),
            _ => ShowUnknownSubcommandAndExit(subcommand)
        };
    }

    private async Task<int> EnableVenv()
    {
        var projectRoot = CommandHelper.FindProjectRoot();
        if (projectRoot == null)
        {
            CommandHelper.PrintError("当前目录不是 Old8Lang 项目");
            return 1;
        }

        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            CommandHelper.PrintError("无法加载项目配置");
            return 1;
        }

        if (config.PackageManager.UseVirtualEnv)
        {
            CommandHelper.PrintInfo("虚拟环境已经启用");
            return 0;
        }

        config.PackageManager.UseVirtualEnv = true;
        config.SaveToDirectory(projectRoot);

        // 创建 packages 目录
        var packagesDir = Path.Combine(projectRoot, config.PackageManager.PackagesDir);
        if (!Directory.Exists(packagesDir))
        {
            Directory.CreateDirectory(packagesDir);
            CommandHelper.PrintSuccess($"创建目录: {config.PackageManager.PackagesDir}");
        }

        CommandHelper.PrintSuccess("虚拟环境已启用");
        CommandHelper.PrintInfo("运行 'old8lang install' 安装依赖到项目本地");

        return await Task.FromResult(0);
    }

    private async Task<int> DisableVenv()
    {
        var projectRoot = CommandHelper.FindProjectRoot();
        if (projectRoot == null)
        {
            CommandHelper.PrintError("当前目录不是 Old8Lang 项目");
            return 1;
        }

        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            CommandHelper.PrintError("无法加载项目配置");
            return 1;
        }

        if (!config.PackageManager.UseVirtualEnv)
        {
            CommandHelper.PrintInfo("虚拟环境已经禁用");
            return 0;
        }

        var confirm = CommandHelper.ReadYesNo("禁用虚拟环境后将使用全局包，确定继续", false);
        if (!confirm)
        {
            CommandHelper.PrintInfo("已取消");
            return 0;
        }

        config.PackageManager.UseVirtualEnv = false;
        config.SaveToDirectory(projectRoot);

        CommandHelper.PrintSuccess("虚拟环境已禁用");
        CommandHelper.PrintInfo("现在将使用全局包");

        return await Task.FromResult(0);
    }

    private async Task<int> ShowStatus()
    {
        var projectRoot = CommandHelper.FindProjectRoot();
        if (projectRoot == null)
        {
            CommandHelper.PrintError("当前目录不是 Old8Lang 项目");
            return 1;
        }

        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            CommandHelper.PrintError("无法加载项目配置");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  虚拟环境状态");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine($"项目名称: {config.Name}");
        Console.WriteLine($"项目版本: {config.Version}");
        Console.WriteLine($"项目根目录: {projectRoot}");
        Console.WriteLine();

        if (config.PackageManager.UseVirtualEnv)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("虚拟环境: 已启用 ✓");
            Console.ResetColor();

            var packagesDir = Path.Combine(projectRoot, config.PackageManager.PackagesDir);
            Console.WriteLine($"包目录: {packagesDir}");
            Console.WriteLine($"自动锁定: {(config.PackageManager.AutoLock ? "是" : "否")}");
            Console.WriteLine($"严格模式: {(config.PackageManager.Strict ? "是" : "否")}");

            if (Directory.Exists(packagesDir))
            {
                var packageCount = Directory.GetDirectories(packagesDir).Length;
                Console.WriteLine($"已安装包数量: {packageCount}");
            }
            else
            {
                Console.WriteLine("已安装包数量: 0 (目录不存在)");
            }

            // 检查锁文件
            var lockFilePath = Path.Combine(projectRoot, LockFile.FileName);
            if (File.Exists(lockFilePath))
            {
                var lockFile = LockFile.LoadFromDirectory(projectRoot);
                Console.WriteLine($"锁文件: 存在 ({lockFile?.Packages.Count ?? 0} 个锁定包)");
            }
            else
            {
                Console.WriteLine("锁文件: 不存在");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("虚拟环境: 未启用");
            Console.ResetColor();
            Console.WriteLine("使用全局包目录");

            var globalDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".old8lang",
                "packages"
            );
            Console.WriteLine($"全局包目录: {globalDir}");
        }

        Console.WriteLine();
        Console.WriteLine("========================================");

        return await Task.FromResult(0);
    }

    private async Task<int> CleanVenv()
    {
        var projectRoot = CommandHelper.FindProjectRoot();
        if (projectRoot == null)
        {
            CommandHelper.PrintError("当前目录不是 Old8Lang 项目");
            return 1;
        }

        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            CommandHelper.PrintError("无法加载项目配置");
            return 1;
        }

        var packagesDir = Path.Combine(projectRoot, config.PackageManager.PackagesDir);

        if (!Directory.Exists(packagesDir))
        {
            CommandHelper.PrintInfo("包目录不存在，无需清理");
            return 0;
        }

        var packageCount = Directory.GetDirectories(packagesDir).Length;

        var confirm = CommandHelper.ReadYesNo($"确定要删除 {packageCount} 个包", false);
        if (!confirm)
        {
            CommandHelper.PrintInfo("已取消");
            return 0;
        }

        try
        {
            Directory.Delete(packagesDir, true);
            CommandHelper.PrintSuccess($"已清理虚拟环境 (删除了 {packageCount} 个包)");
            CommandHelper.PrintInfo("运行 'old8lang install' 重新安装依赖");

            return await Task.FromResult(0);
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"清理失败: {ex.Message}");
            return 1;
        }
    }

    private int ShowHelpAndExit()
    {
        Console.WriteLine(Help);
        return 0;
    }

    private int ShowUnknownSubcommandAndExit(string subcommand)
    {
        CommandHelper.PrintError($"未知子命令: {subcommand}");
        Console.WriteLine(Help);
        return 1;
    }
}
