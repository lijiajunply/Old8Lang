using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang install - 安装所有依赖命令
/// </summary>
public class InstallCommand : ICommand
{
    public string Name => "install";
    public string Description => "安装项目的所有依赖";

    public string Help => @"
用法: old8lang install [选项]

选项:
  --production           只安装生产依赖（跳过 devDependencies）
  --frozen-lockfile      使用精确的锁文件版本，不更新
  -h, --help             显示帮助信息

示例:
  old8lang install                  # 安装所有依赖
  old8lang install --production     # 只安装生产依赖
  old8lang install --frozen-lockfile # 使用锁文件精确版本
";

    public async Task<int> ExecuteAsync(string[] args)
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

        var productionOnly = args.Contains("--production");
        var frozenLockfile = args.Contains("--frozen-lockfile");

        Console.WriteLine($"安装 {config.Name}@{config.Version} 的依赖...");
        Console.WriteLine();

        // 加载锁文件
        LockFile? lockFile = null;
        if (frozenLockfile || File.Exists(Path.Combine(projectRoot, LockFile.FileName)))
        {
            lockFile = LockFile.LoadFromDirectory(projectRoot);
            if (lockFile != null)
            {
                CommandHelper.PrintInfo("使用锁文件中的版本");
            }
        }

        var packagesDir = Path.Combine(projectRoot, config.PackageManager.PackagesDir);
        Directory.CreateDirectory(packagesDir);

        int installedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        // 安装生产依赖
        foreach (var (packageName, versionRange) in config.Dependencies)
        {
            var result = await InstallPackage(packagesDir, packageName, versionRange, lockFile);
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
        if (!productionOnly && config.DevDependencies.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("安装开发依赖...");

            foreach (var (packageName, versionRange) in config.DevDependencies)
            {
                var result = await InstallPackage(packagesDir, packageName, versionRange, lockFile);
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

        // 更新锁文件
        if (!frozenLockfile)
        {
            var newLockFile = LockFile.Generate(config, packagesDir);
            newLockFile.SaveToDirectory(projectRoot);
            CommandHelper.PrintInfo("已更新锁文件");
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

    private enum InstallResult
    {
        Success,
        Skipped,
        Failed
    }

    private async Task<InstallResult> InstallPackage(
        string packagesDir,
        string packageName,
        string versionRange,
        LockFile? lockFile)
    {
        try
        {
            // 确定要安装的版本
            string version;
            if (lockFile != null && lockFile.Packages.TryGetValue(packageName, out var lockInfo))
            {
                version = lockInfo.Version;
            }
            else
            {
                // 解析版本范围，取具体版本
                version = versionRange.TrimStart('^', '~', '>').Trim();
                if (version == "*" || version == versionRange.TrimStart('^', '~', '>').Trim())
                {
                    version = "1.0.0"; // 默认版本
                }
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
            File.WriteAllText(packageJsonPath, packageJson);

            var packageFilePath = Path.Combine(packageDir, $"{packageName}.old8");
            var packageContent = $"// {packageName} v{version}\nPrintLine(\"{packageName} loaded\")";
            File.WriteAllText(packageFilePath, packageContent);

            Console.WriteLine(" 完成");

            return InstallResult.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" 失败: {ex.Message}");
            return InstallResult.Failed;
        }
    }
}
