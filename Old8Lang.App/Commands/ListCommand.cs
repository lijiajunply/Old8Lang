using Old8Lang.App.Services;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang list - 列出已安装包命令
/// </summary>
public class ListCommand : ICommand
{
    public string Name => "list";
    public string Description => "列出已安装的依赖包";

    public string Help => @"
用法: old8lang list [选项]

选项:
  --all                  列出全局已安装的所有包
  -h, --help             显示帮助信息

示例:
  old8lang list          # 列出项目依赖及安装状态
  old8lang list --all    # 列出全局包目录中的所有已安装包
";

    public int Execute(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var showAll = args.Contains("--all");
        var projectRoot = CommandHelper.FindProjectRoot();

        if (showAll && projectRoot == null)
        {
            using var globalService = new PackageService(Directory.GetCurrentDirectory());
            var globalInstalled = globalService.GetInstalledPackagesAsync().GetAwaiter().GetResult().ToList();
            if (globalInstalled.Count == 0)
            {
                CommandHelper.PrintInfo("全局目录中未安装任何包");
                return 0;
            }

            Console.WriteLine($"全局已安装的包 ({globalService.PackagesDir}):");
            foreach (var pkg in globalInstalled)
            {
                Console.WriteLine($"  - {pkg.Id}@{pkg.Version}");
            }
            return 0;
        }

        if (projectRoot == null)
        {
            CommandHelper.PrintError("当前目录不是 Old8Lang 项目");
            CommandHelper.PrintInfo("提示: 使用 'old8lang list --all' 可查看全局已安装的包");
            return 1;
        }

        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            CommandHelper.PrintError("无法加载项目配置");
            return 1;
        }

        using var service = new PackageService(projectRoot, config);

        Console.WriteLine();
        Console.WriteLine($"{config.ProjectName}@{config.Version} {projectRoot}");
        Console.WriteLine($"包仓库: {service.RegistryUrl}");
        Console.WriteLine();

        var installedPackages = service.GetInstalledPackagesAsync().GetAwaiter().GetResult().ToList();

        if (showAll)
        {
            if (installedPackages.Count == 0)
            {
                CommandHelper.PrintInfo("未安装任何包");
            }
            else
            {
                Console.WriteLine($"全部已安装包 ({service.PackagesDir}):");
                foreach (var pkg in installedPackages)
                {
                    Console.WriteLine($"  ├── {pkg.Id}@{pkg.Version}");
                }
                Console.WriteLine();
            }
        }

        var prodDeps = config.References.Where(r => !r.IsDevDependency).ToList();
        if (prodDeps.Count > 0)
        {
            Console.WriteLine("依赖 (dependencies):");
            foreach (var dep in prodDeps)
            {
                var installed = installedPackages.FirstOrDefault(p =>
                    p.Id.Equals(dep.PackageId, StringComparison.OrdinalIgnoreCase));
                if (installed != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"├── {dep.PackageId}@{installed.Version} (要求: {dep.Version})");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"├── {dep.PackageId}@{dep.Version} (未安装)");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        var devDeps = config.References.Where(r => r.IsDevDependency).ToList();
        if (devDeps.Count > 0)
        {
            Console.WriteLine("开发依赖 (devDependencies):");
            foreach (var dep in devDeps)
            {
                var installed = installedPackages.FirstOrDefault(p =>
                    p.Id.Equals(dep.PackageId, StringComparison.OrdinalIgnoreCase));
                if (installed != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"├── {dep.PackageId}@{installed.Version} (要求: {dep.Version})");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"├── {dep.PackageId}@{dep.Version} (未安装)");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        if (prodDeps.Count == 0 && devDeps.Count == 0 && !showAll)
        {
            CommandHelper.PrintInfo("当前项目没有声明任何依赖");
        }

        var langInfo = Apis.ReadJson();
        Console.WriteLine($"Old8Lang: {langInfo.Var}");

        return 0;
    }
}
