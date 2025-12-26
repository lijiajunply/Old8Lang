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
  --tree                 树形显示依赖关系（待实现）
  -h, --help             显示帮助信息

示例:
  old8lang list          # 列出项目依赖
";

    public async Task<int> ExecuteAsync(string[] args)
    {
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

        // 列出项目信息
        Console.WriteLine();
        Console.WriteLine($"{config.ProjectName}@{config.Version} {projectRoot}");
        Console.WriteLine();

        // 列出已安装的包
        var packagesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".old8lang",
            "packages"
        );
        if (!Directory.Exists(packagesDir))
        {
            CommandHelper.PrintInfo("未安装任何包");
            CommandHelper.PrintInfo("运行 'old8lang install' 安装依赖");
            return 0;
        }

        var installedPackages = GetInstalledPackages(packagesDir);

        if (installedPackages.Count == 0)
        {
            CommandHelper.PrintInfo("未安装任何包");
            return 0;
        }

        // 显示依赖
        var prodDeps = config.References.Where(r => !r.IsDevDependency).ToList();
        if (prodDeps.Count > 0)
        {
            Console.WriteLine("依赖 (dependencies):");
            foreach (var dep in prodDeps)
            {
                var installed = installedPackages.FirstOrDefault(p => p.name == dep.PackageId);
                if (installed.name != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"├── {dep.PackageId}@{installed.version}");
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

        // 显示开发依赖
        var devDeps = config.References.Where(r => r.IsDevDependency).ToList();
        if (devDeps.Count > 0)
        {
            Console.WriteLine("开发依赖 (devDependencies):");
            foreach (var dep in devDeps)
            {
                var installed = installedPackages.FirstOrDefault(p => p.name == dep.PackageId);
                if (installed.name != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"├── {dep.PackageId}@{installed.version}");
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

        // 显示环境信息
        var langInfo = Apis.ReadJson();
        Console.WriteLine($"Old8Lang: {langInfo.Var}");

        return await Task.FromResult(0);
    }

    private List<(string name, string version)> GetInstalledPackages(string packagesDir)
    {
        var packages = new List<(string name, string version)>();

        if (!Directory.Exists(packagesDir))
            return packages;

        var packageDirs = Directory.GetDirectories(packagesDir);

        foreach (var packagePath in packageDirs)
        {
            var dirName = Path.GetFileName(packagePath);
            var parts = dirName.Split('@');

            if (parts.Length == 2)
            {
                packages.Add((parts[0], parts[1]));
            }
            else
            {
                // 非版本化目录
                var packageJsonPath = Path.Combine(packagePath, "package.json");
                string version = "unknown";

                if (File.Exists(packageJsonPath))
                {
                    try
                    {
                        var json = File.ReadAllText(packageJsonPath);
                        var match = System.Text.RegularExpressions.Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
                        if (match.Success)
                        {
                            version = match.Groups[1].Value;
                        }
                    }
                    catch
                    {
                        // 忽略解析错误
                    }
                }

                packages.Add((dirName, version));
            }
        }

        return packages;
    }
}
