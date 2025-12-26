using Old8Lang.PackageManager.Core.Models;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang install - 安装依赖包命令（合并了原 add 和 install 功能）
/// </summary>
public class InstallCommand : ICommand
{
    public string Name => "install";
    public string Description => "安装依赖包到项目";

    public string Help => @"
用法: old8lang install [包名] [选项]

参数:
  [包名]                 要安装的包名称，可以包含版本号（可选）
                         不指定包名时安装所有依赖

选项:
  --dev                  添加为开发依赖
  --production           只安装生产依赖（跳过 devDependencies）
  -h, --help             显示帮助信息

示例:
  old8lang install                     # 安装所有依赖
  old8lang install Logger              # 添加并安装最新版本
  old8lang install Logger@1.2.0        # 添加并安装指定版本
  old8lang install Logger@^1.2.0       # 添加并安装兼容版本
  old8lang install --dev TestFramework # 添加为开发依赖
  old8lang install --production        # 只安装生产依赖
";

    public async Task<int> ExecuteAsync(string[] args)
    {
        // 过滤选项，获取包名
        var isDev = args.Contains("--dev");
        var productionOnly = args.Contains("--production");

        var packageSpec = args.FirstOrDefault(a => !a.StartsWith("--") && !a.StartsWith("-"));

        // 如果指定了包名，执行添加包的逻辑
        if (!string.IsNullOrEmpty(packageSpec))
        {
            return await InstallSinglePackage(packageSpec, isDev);
        }

        // 否则，执行安装所有依赖的逻辑
        return await InstallAllDependencies(productionOnly);
    }

    /// <summary>
    /// 安装单个包（原 add 命令的功能）
    /// </summary>
    private async Task<int> InstallSinglePackage(string packageSpec, bool isDev)
    {
        // 解析包名和版本
        var (packageName, version) = ParsePackageSpec(packageSpec);

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
        var existingDep = config.References.FirstOrDefault(d => d.PackageId == packageName);
        if (existingDep != null)
        {
            CommandHelper.PrintWarning($"包 '{packageName}' 已存在于{(existingDep.IsDevDependency ? "开发" : "")}依赖中");
            var shouldUpdate = CommandHelper.ReadYesNo("是否更新版本");
            if (!shouldUpdate)
            {
                return 0;
            }
            // 移除旧的依赖
            config.References.Remove(existingDep);
        }

        // 安装包
        Console.WriteLine($"正在安装 {packageName}@{version}...");

        var success = await InstallPackageToDirectory(projectRoot, config, packageName, version);

        if (!success)
        {
            CommandHelper.PrintError($"安装包 '{packageName}' 失败");
            return 1;
        }

        // 更新项目配置
        config.References.Add(new PackageReference { PackageId = packageName, Version = version, IsDevDependency = isDev });
        config.SaveToDirectory(projectRoot);

        CommandHelper.PrintSuccess($"已添加 {packageName}@{version}");
        CommandHelper.PrintInfo(isDev ? "已保存到 devDependencies" : "已保存到 dependencies");

        return 0;
    }

    /// <summary>
    /// 安装所有依赖（原 install 命令的功能）
    /// </summary>
    private async Task<int> InstallAllDependencies(bool productionOnly)
    {
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

        Console.WriteLine($"安装 {config.ProjectName}@{config.Version} 的依赖...");
        Console.WriteLine();

        var packagesDir = GetPackagesDirectory();
        Directory.CreateDirectory(packagesDir);

        int installedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        // 安装生产依赖
        var prodDeps = config.References.Where(r => !r.IsDevDependency).ToList();
        foreach (var dep in prodDeps)
        {
            var result = await InstallPackage(packagesDir, dep.PackageId, dep.Version);
            switch (result)
            {
                case InstallResult.Success:
                    installedCount++;
                    break;
                case InstallResult.Skipped:
                    skippedCount++;
                    break;
                case InstallResult.Failed:
                    failedCount++;
                    break;
            }
        }

        // 安装开发依赖
        var devDeps = config.References.Where(r => r.IsDevDependency).ToList();
        if (!productionOnly && devDeps.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("安装开发依赖...");

            foreach (var dep in devDeps)
            {
                var result = await InstallPackage(packagesDir, dep.PackageId, dep.Version);
                switch (result)
                {
                    case InstallResult.Success:
                        installedCount++;
                        break;
                    case InstallResult.Skipped:
                        skippedCount++;
                        break;
                    case InstallResult.Failed:
                        failedCount++;
                        break;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        CommandHelper.PrintSuccess($"安装完成: {installedCount} 个包已安装");
        if (skippedCount > 0)
        {
            CommandHelper.PrintInfo($"跳过: {skippedCount} 个包（已存在）");
        }
        if (failedCount > 0)
        {
            CommandHelper.PrintError($"失败: {failedCount} 个包");
        }
        Console.WriteLine("========================================");

        return failedCount > 0 ? 1 : 0;
    }

    /// <summary>
    /// 解析包规格（包名@版本）
    /// </summary>
    private (string name, string version) ParsePackageSpec(string spec)
    {
        var parts = spec.Split('@', 2);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }

        return (parts[0], "*");
    }

    /// <summary>
    /// 安装包到指定目录
    /// </summary>
    private async Task<bool> InstallPackageToDirectory(string projectRoot, ProjectConfig config, string packageName, string version)
    {
        try
        {
            var packagesDir = GetPackagesDirectory();

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

    private enum InstallResult
    {
        Success,
        Skipped,
        Failed
    }

    private async Task<InstallResult> InstallPackage(
        string packagesDir,
        string packageName,
        string versionRange)
    {
        try
        {
            // 解析版本范围，取具体版本
            var version = versionRange.TrimStart('^', '~', '>').Trim();
            if (version == "*" || version == versionRange.TrimStart('^', '~', '>').Trim())
            {
                version = "1.0.0"; // 默认版本
            }

            var packageDir = Path.Combine(packagesDir, $"{packageName}@{version}");

            // 检查是否已存在
            if (Directory.Exists(packageDir))
            {
                Console.WriteLine($"  {packageName}@{version} - 已存在");
                return InstallResult.Skipped;
            }

            Console.Write($"  安装 {packageName}@{version}...");

            // TODO: 实现实际的包下载逻辑
            await Task.Delay(200); // 模拟下载

            Directory.CreateDirectory(packageDir);

            // 创建包文件
            var packageJsonPath = Path.Combine(packageDir, "package.json");
            var packageJson = $$"""
                {
                  "name": "{{packageName}}",
                  "version": "{{version}}",
                  "description": "{{packageName}} package"
                }
                """;
            await File.WriteAllTextAsync(packageJsonPath, packageJson);

            var packageFilePath = Path.Combine(packageDir, $"{packageName}.old8");
            var packageContent = $"// {packageName} v{version}\nPrintLine(\"{packageName} loaded\")";
            await File.WriteAllTextAsync(packageFilePath, packageContent);

            Console.WriteLine(" 完成");

            return InstallResult.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" 失败: {ex.Message}");
            return InstallResult.Failed;
        }
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
