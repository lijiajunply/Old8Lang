using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang remove - 移除包命令
/// </summary>
public class RemoveCommand : ICommand
{
    public string Name => "remove";
    public string Description => "从项目移除依赖包";

    public string Help => @"
用法: old8lang remove <包名> [选项]

参数:
  <包名>                 要移除的包名称

选项:
  --dev                  从开发依赖移除
  -h, --help             显示帮助信息

示例:
  old8lang remove Logger             # 移除依赖
  old8lang remove --dev TestFramework # 移除开发依赖
";

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            CommandHelper.PrintError("错误: 缺少包名参数");
            Console.WriteLine(Help);
            return 1;
        }

        // 过滤选项，获取包名
        var isDev = args.Contains("--dev");

        var packageName = args.FirstOrDefault(a => !a.StartsWith("--") && !a.StartsWith("-"));
        if (string.IsNullOrEmpty(packageName))
        {
            CommandHelper.PrintError("错误: 缺少包名参数");
            Console.WriteLine(Help);
            return 1;
        }

        // 检查项目配置
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

        // 检查包是否存在
        var existingDep = config.References.FirstOrDefault(d => d.PackageId == packageName && d.IsDevDependency == isDev);
        if (existingDep == null)
        {
            CommandHelper.PrintWarning($"包 '{packageName}' 不在{(isDev ? "开发" : "")}依赖中");
            return 1;
        }

        // 确认移除
        var confirm = CommandHelper.ReadYesNo($"确定要移除 {packageName}");
        if (!confirm)
        {
            CommandHelper.PrintInfo("已取消");
            return 0;
        }

        // 从配置中移除
        var version = existingDep.Version;
        config.References.Remove(existingDep);
        config.SaveToDirectory(projectRoot);

        // 删除包目录
        var packagesDir = GetPackagesDirectory();
        var packageDirs = Directory.GetDirectories(packagesDir, $"{packageName}@*");

        foreach (var packageDir in packageDirs)
        {
            try
            {
                Directory.Delete(packageDir, true);
                CommandHelper.PrintInfo($"删除目录: {Path.GetFileName(packageDir)}");
            }
            catch (Exception ex)
            {
                CommandHelper.PrintWarning($"删除目录失败: {ex.Message}");
            }
        }

        CommandHelper.PrintSuccess($"已移除 {packageName}@{version}");

        return await Task.FromResult(0);
    }

    private static string GetPackagesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".old8lang",
            "packages"
        );
    }
}
