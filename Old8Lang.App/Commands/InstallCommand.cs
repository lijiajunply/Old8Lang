using Old8Lang.PackageManager.Core.Models;
using Old8Lang.ProjectManagement;
using Old8Lang.App.Services;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang install - 从 Old8Lang.PackageManager.Server 安装依赖包命令
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
  -r, --registry <地址>  指定远程 Old8Lang.PackageManager.Server 地址
  -k, --api-key <密钥>   指定访问私有仓库的 API 密钥
  -h, --help             显示帮助信息

示例:
  old8lang install                                      # 安装所有依赖
  old8lang install Logger                               # 添加并安装最新版本
  old8lang install Logger@1.2.0                         # 添加并安装指定版本
  old8lang install Logger@^1.2.0                        # 添加并安装兼容版本
  old8lang install --dev TestFramework                  # 添加为开发依赖
  old8lang install --registry http://localhost:5000 pkg # 从自建私有仓库安装
";

    public int Execute(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var isDev = args.Contains("--dev");
        var productionOnly = args.Contains("--production");
        var registryOverride = GetOptionValue(args, "-r", "--registry");
        var apiKeyOverride = GetOptionValue(args, "-k", "--api-key");

        var packageSpec = GetPositionalPackageSpec(args);

        if (!string.IsNullOrEmpty(packageSpec))
        {
            return InstallSinglePackage(packageSpec, isDev, registryOverride, apiKeyOverride);
        }

        return InstallAllDependencies(productionOnly, registryOverride, apiKeyOverride);
    }

    private int InstallSinglePackage(string packageSpec, bool isDev, string? registryOverride, string? apiKeyOverride)
    {
        var (packageName, requestedVersion) = ParsePackageSpec(packageSpec);

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

        var existingDep = config.References.FirstOrDefault(d => d.PackageId == packageName);
        if (existingDep != null)
        {
            CommandHelper.PrintWarning($"包 '{packageName}' 已存在于{(existingDep.IsDevDependency ? "开发" : "")}依赖中");
            var shouldUpdate = CommandHelper.ReadYesNo("是否更新版本");
            if (!shouldUpdate)
            {
                return 0;
            }
            config.References.Remove(existingDep);
        }

        using var packageService = new PackageService(projectRoot, config, registryOverride, apiKeyOverride);
        Console.WriteLine($"正在从 {packageService.RegistryUrl} 安装 {packageName}@{requestedVersion}...");

        var result = packageService.InstallPackageAsync(packageName, requestedVersion, isDev).GetAwaiter().GetResult();

        if (!result.Success)
        {
            CommandHelper.PrintError($"安装包 '{packageName}' 失败: {result.Message}");
            return 1;
        }

        if (result.Skipped)
        {
            CommandHelper.PrintInfo(result.Message);
        }
        else
        {
            CommandHelper.PrintSuccess($"✓ {result.Message}");
        }

        // 若用户未指定版本范围（*），记录实际解析到的兼容范围 ^<version>
        var versionToSave = requestedVersion == "*" && !string.IsNullOrWhiteSpace(result.Version)
            ? $"^{result.Version}"
            : requestedVersion;

        config.References.Add(new PackageReference
        {
            PackageId = packageName,
            Version = versionToSave,
            IsDevDependency = isDev
        });
        config.SaveToDirectory(projectRoot);

        CommandHelper.PrintSuccess($"已添加 {packageName}@{versionToSave}");
        CommandHelper.PrintInfo(isDev ? "已保存到 devDependencies" : "已保存到 dependencies");

        return 0;
    }

    private int InstallAllDependencies(bool productionOnly, string? registryOverride, string? apiKeyOverride)
    {
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

        using var packageService = new PackageService(projectRoot, config, registryOverride, apiKeyOverride);

        Console.WriteLine($"安装 {config.ProjectName}@{config.Version} 的依赖...");
        Console.WriteLine($"包仓库: {packageService.RegistryUrl}");
        Console.WriteLine();

        Directory.CreateDirectory(packageService.PackagesDir);

        int installedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        var prodDeps = config.References.Where(r => !r.IsDevDependency).ToList();
        foreach (var dep in prodDeps)
        {
            var result = InstallSingleDependency(packageService, dep.PackageId, dep.Version, isDev: false);
            switch (result)
            {
                case InstallOutcome.Success: installedCount++; break;
                case InstallOutcome.Skipped: skippedCount++; break;
                case InstallOutcome.Failed: failedCount++; break;
            }
        }

        var devDeps = config.References.Where(r => r.IsDevDependency).ToList();
        if (!productionOnly && devDeps.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("安装开发依赖...");

            foreach (var dep in devDeps)
            {
                var result = InstallSingleDependency(packageService, dep.PackageId, dep.Version, isDev: true);
                switch (result)
                {
                    case InstallOutcome.Success: installedCount++; break;
                    case InstallOutcome.Skipped: skippedCount++; break;
                    case InstallOutcome.Failed: failedCount++; break;
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

    private static (string name, string version) ParsePackageSpec(string spec)
    {
        var parts = spec.Split('@', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], "*");
    }

    private enum InstallOutcome
    {
        Success,
        Skipped,
        Failed
    }

    private static InstallOutcome InstallSingleDependency(
        PackageService packageService,
        string packageName,
        string versionRange,
        bool isDev)
    {
        try
        {
            Console.Write($"  安装 {packageName}@{versionRange}...");
            var result = packageService.InstallPackageAsync(packageName, versionRange, isDev).GetAwaiter().GetResult();

            if (!result.Success)
            {
                Console.WriteLine($" 失败: {result.Message}");
                return InstallOutcome.Failed;
            }

            if (result.Skipped)
            {
                Console.WriteLine($" 已存在 ({result.Version})");
                return InstallOutcome.Skipped;
            }

            Console.WriteLine($" 完成 ({result.Version})");
            return InstallOutcome.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" 失败: {ex.Message}");
            return InstallOutcome.Failed;
        }
    }

    private static string? GetOptionValue(string[] args, params string[] optionNames)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (optionNames.Contains(args[i]) && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static string? GetPositionalPackageSpec(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-r" or "--registry" or "-k" or "--api-key")
            {
                i++;
                continue;
            }

            if (!arg.StartsWith('-'))
            {
                return arg;
            }
        }

        return null;
    }
}
