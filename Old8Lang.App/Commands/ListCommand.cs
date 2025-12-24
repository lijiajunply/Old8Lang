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
  --global               列出全局安装的包
  --tree                 树形显示依赖关系（待实现）
  -h, --help             显示帮助信息

示例:
  old8lang list          # 列出项目依赖
  old8lang list --global # 列出全局包
";

    public async Task<int> ExecuteAsync(string[] args)
    {
        var isGlobal = args.Contains("--global");

        if (isGlobal)
        {
            return await ListGlobal();
        }

        // 检查项目配置
        var projectRoot = CommandHelper.FindProjectRoot();
        if (projectRoot == null)
        {
            CommandHelper.PrintError("当前目录不是 Old8Lang 项目");
            CommandHelper.PrintInfo("提示: 使用 'old8lang list --global' 列出全局包");
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
        Console.WriteLine($"{config.Name}@{config.Version} {projectRoot}");
        Console.WriteLine();

        // 列出已安装的包
        var packagesDir = Path.Combine(projectRoot, config.PackageManager.PackagesDir);
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
        if (config.Dependencies.Count > 0)
        {
            Console.WriteLine("依赖 (dependencies):");
            foreach (var (name, versionRange) in config.Dependencies)
            {
                var installed = installedPackages.FirstOrDefault(p => p.name == name);
                if (installed.name != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"├── {name}@{installed.version}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"├── {name}@{versionRange} (未安装)");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        // 显示开发依赖
        if (config.DevDependencies.Count > 0)
        {
            Console.WriteLine("开发依赖 (devDependencies):");
            foreach (var (name, versionRange) in config.DevDependencies)
            {
                var installed = installedPackages.FirstOrDefault(p => p.name == name);
                if (installed.name != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"├── {name}@{installed.version}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"├── {name}@{versionRange} (未安装)");
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        // 显示环境信息
        var langInfo = Apis.ReadJson();
        Console.WriteLine($"Old8Lang: {langInfo.Var}");
        Console.WriteLine($"虚拟环境: {(config.PackageManager.UseVirtualEnv ? "已启用" : "未启用")}");

        return await Task.FromResult(0);
    }

    private async Task<int> ListGlobal()
    {
        var globalDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".old8lang",
            "packages"
        );

        if (!Directory.Exists(globalDir))
        {
            CommandHelper.PrintInfo("未安装任何全局包");
            return await Task.FromResult(0);
        }

        var packages = Directory.GetDirectories(globalDir);

        if (packages.Length == 0)
        {
            CommandHelper.PrintInfo("未安装任何全局包");
            return await Task.FromResult(0);
        }

        Console.WriteLine();
        Console.WriteLine($"全局包 {globalDir}");
        Console.WriteLine();

        foreach (var packagePath in packages)
        {
            var packageName = Path.GetFileName(packagePath);
            var packageJsonPath = Path.Combine(packagePath, "package.json");

            string version = "unknown";
            if (File.Exists(packageJsonPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(packageJsonPath);
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

            Console.WriteLine($"├── {packageName}@{version}");
        }

        Console.WriteLine();
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
