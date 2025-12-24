using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang add - 添加包命令
/// </summary>
public class AddCommand : ICommand
{
    public string Name => "add";
    public string Description => "添加依赖包到项目";

    public string Help => @"
用法: old8lang add <包名> [选项]

参数:
  <包名>                 要添加的包名称，可以包含版本号

选项:
  --dev                  添加为开发依赖
  --global               安装到全局（不加入 dependencies）
  --save                 保存到项目配置（默认）
  -h, --help             显示帮助信息

示例:
  old8lang add Logger              # 添加最新版本
  old8lang add Logger@1.2.0        # 添加指定版本
  old8lang add Logger@^1.2.0       # 添加兼容版本
  old8lang add --dev TestFramework # 添加开发依赖
  old8lang add --global Logger     # 全局安装
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
        var isGlobal = args.Contains("--global");

        var packageSpec = args.FirstOrDefault(a => !a.StartsWith("--") && !a.StartsWith("-"));
        if (string.IsNullOrEmpty(packageSpec))
        {
            CommandHelper.PrintError("错误: 缺少包名参数");
            Console.WriteLine(Help);
            return 1;
        }

        // 解析包名和版本
        var (packageName, version) = ParsePackageSpec(packageSpec);

        if (isGlobal)
        {
            return await InstallGlobal(packageName, version);
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

        // 检查包是否已存在
        var dependencies = isDev ? config.DevDependencies : config.Dependencies;
        if (dependencies.ContainsKey(packageName))
        {
            CommandHelper.PrintWarning($"包 '{packageName}' 已存在于{(isDev ? "开发" : "")}依赖中");
            var shouldUpdate = CommandHelper.ReadYesNo("是否更新版本");
            if (!shouldUpdate)
            {
                return 0;
            }
        }

        // 安装包
        Console.WriteLine($"正在添加 {packageName}@{version}...");

        // TODO: 实现实际的包下载和安装逻辑
        // 目前只是模拟安装过程
        var success = await InstallPackage(projectRoot, config, packageName, version);

        if (!success)
        {
            CommandHelper.PrintError($"安装包 '{packageName}' 失败");
            return 1;
        }

        // 更新项目配置
        dependencies[packageName] = version;
        config.SaveToDirectory(projectRoot);

        // 更新锁文件
        var lockFile = LockFile.Generate(config, Path.Combine(projectRoot, config.PackageManager.PackagesDir));
        lockFile.SaveToDirectory(projectRoot);

        CommandHelper.PrintSuccess($"已添加 {packageName}@{version}");

        CommandHelper.PrintInfo(isDev ? "已保存到 devDependencies" : "已保存到 dependencies");

        return 0;
    }

    private (string name, string version) ParsePackageSpec(string spec)
    {
        var parts = spec.Split('@', 2);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }

        return (parts[0], "*");
    }

    private async Task<bool> InstallPackage(string projectRoot, ProjectConfig config, string packageName,
        string version)
    {
        try
        {
            var packagesDir = Path.Combine(projectRoot, config.PackageManager.PackagesDir);

            // 确定版本号
            var resolvedVersion = version == "*" ? "1.0.0" : version.TrimStart('^', '~', '>');

            // 包目录
            var packageDir = Path.Combine(packagesDir, $"{packageName}@{resolvedVersion}");

            // 检查是否已存在
            if (Directory.Exists(packageDir))
            {
                CommandHelper.PrintInfo($"包 '{packageName}@{resolvedVersion}' 已存在，跳过安装");
                return true;
            }

            // 模拟下载过程
            Console.WriteLine($"  下载 {packageName}@{resolvedVersion}...");
            await Task.Delay(500); // 模拟网络延迟

            // TODO: 实现实际的包下载逻辑
            // 1. 从包注册表获取包信息
            // 2. 下载包文件
            // 3. 解压到包目录
            // 4. 处理依赖

            // 目前只是创建占位目录
            Directory.CreateDirectory(packageDir);

            // 创建占位包文件
            var packageJsonPath = Path.Combine(packageDir, "package.json");
            var packageJson = $$"""
                                {
                                  "name": "{{packageName}}",
                                  "version": "{{resolvedVersion}}",
                                  "description": "{{packageName}} package"
                                }
                                """;
            await File.WriteAllTextAsync(packageJsonPath, packageJson);

            var packageFilePath = Path.Combine(packageDir, $"{packageName}.old8");
            var packageContent = $"// {packageName} v{resolvedVersion}\nPrintLine(\"{packageName} loaded\")";
            await File.WriteAllTextAsync(packageFilePath, packageContent);

            Console.WriteLine("  安装完成");

            return true;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"安装失败: {ex.Message}");
            return false;
        }
    }

    private async Task<int> InstallGlobal(string packageName, string version)
    {
        CommandHelper.PrintInfo($"安装 {packageName} 到全局目录...");

        var globalDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".old8lang",
            "packages",
            packageName
        );

        if (Directory.Exists(globalDir))
        {
            CommandHelper.PrintWarning($"全局包 '{packageName}' 已存在");
            var shouldUpdate = CommandHelper.ReadYesNo("是否更新");
            if (!shouldUpdate)
            {
                return 0;
            }
        }

        // TODO: 实现实际的全局安装逻辑
        Console.WriteLine($"  下载 {packageName}@{version}...");
        await Task.Delay(500);

        Directory.CreateDirectory(globalDir);

        var packageJsonPath = Path.Combine(globalDir, "package.json");
        var resolvedVersion = version == "*" ? "1.0.0" : version.TrimStart('^', '~', '>');
        var packageJson = $$"""
                            {
                              "name": "{{packageName}}",
                              "version": "{{resolvedVersion}}",
                              "description": "{{packageName}} package"
                            }
                            """;
        await File.WriteAllTextAsync(packageJsonPath, packageJson);

        CommandHelper.PrintSuccess($"已全局安装 {packageName}");

        return 0;
    }
}