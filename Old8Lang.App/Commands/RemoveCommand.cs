using Old8Lang.App.Services;
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
  -y, --yes              跳过确认提示
  -h, --help             显示帮助信息

示例:
  old8lang remove Logger             # 移除依赖
  old8lang remove --dev TestFramework # 移除开发依赖
";

    public int Execute(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        if (args.Length == 0)
        {
            CommandHelper.PrintError("错误: 缺少包名参数");
            Console.WriteLine(Help);
            return 1;
        }

        var isDev = args.Contains("--dev");
        var skipConfirm = args.Contains("-y") || args.Contains("--yes");

        var packageName = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (string.IsNullOrEmpty(packageName))
        {
            CommandHelper.PrintError("错误: 缺少包名参数");
            Console.WriteLine(Help);
            return 1;
        }

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

        var existingDep = config.References.FirstOrDefault(d =>
            d.PackageId.Equals(packageName, StringComparison.OrdinalIgnoreCase) &&
            d.IsDevDependency == isDev);
        if (existingDep == null)
        {
            CommandHelper.PrintWarning($"包 '{packageName}' 不在{(isDev ? "开发" : "")}依赖中");
            return 1;
        }

        if (!skipConfirm)
        {
            var confirm = CommandHelper.ReadYesNo($"确定要移除 {packageName}");
            if (!confirm)
            {
                CommandHelper.PrintInfo("已取消");
                return 0;
            }
        }

        var version = existingDep.Version;
        config.References.Remove(existingDep);
        config.SaveToDirectory(projectRoot);

        using var service = new PackageService(projectRoot, config);
        var removedCount = service.UninstallAllVersionsAsync(packageName).GetAwaiter().GetResult();
        if (removedCount > 0)
        {
            CommandHelper.PrintInfo($"已卸载本地 {removedCount} 个版本目录");
        }

        CommandHelper.PrintSuccess($"已移除 {packageName}@{version}");

        return 0;
    }
}
