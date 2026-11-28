using Old8Lang.App.Services;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang registry - 管理唯一远程 Old8Lang.PackageManager.Server 地址与认证凭证
/// </summary>
public class RegistryCommand : ICommand
{
    public string Name => "registry";
    public string Description => "管理远程 Old8Lang 包仓库地址与 API 密钥";

    public string Help => @"
用法: old8lang registry <子命令> [参数]

子命令:
  get                    显示当前生效的远程包仓库地址与配置来源
  set <地址> [--project] 设置远程 Old8Lang.PackageManager.Server 地址
                         默认写入全局配置 ~/.old8lang/config.json；加 --project 写入当前项目 o8package.json
  reset [--project]      重置为官方默认包仓库地址 (https://package.old8lang.site)
  test [地址]            测试与远程 Old8Lang.PackageManager.Server 的连通性
  set-key <密钥>         保存发布/管理用的 API Key 到全局配置 (~/.old8lang/config.json)
  clear-key              清除全局配置中保存的 API Key

示例:
  old8lang registry get
  old8lang registry set http://localhost:5000
  old8lang registry set https://pkg.my-company.internal --project
  old8lang registry set-key <YOUR_API_KEY>
  old8lang registry test
  old8lang registry reset
";

    public int Execute(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var subCommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        return subCommand switch
        {
            "get" => ExecuteGet(),
            "set" => ExecuteSet(subArgs),
            "reset" => ExecuteReset(subArgs),
            "test" => ExecuteTest(subArgs),
            "set-key" => ExecuteSetKey(subArgs),
            "clear-key" => ExecuteClearKey(),
            _ => UnknownSubCommand(subCommand)
        };
    }

    private static int ExecuteGet()
    {
        var projectRoot = CommandHelper.FindProjectRoot();
        var projectConfig = projectRoot != null ? ProjectConfig.LoadFromDirectory(projectRoot) : null;
        var globalConfig = GlobalPackageConfig.Load();
        var effectiveRegistry = GlobalPackageConfig.ResolveRegistryUrl(projectConfig: projectConfig);
        var effectiveApiKey = GlobalPackageConfig.ResolveApiKey();

        Console.WriteLine($"当前生效仓库: {effectiveRegistry}");
        Console.WriteLine($"官方默认仓库: {GlobalPackageConfig.DefaultRegistryUrl}");
        Console.WriteLine($"全局配置文件: {GlobalPackageConfig.GetDefaultConfigPath()}");
        Console.WriteLine($"全局配置仓库: {globalConfig.Registry ?? "(未设置)"}");

        var envRegistry = Environment.GetEnvironmentVariable(GlobalPackageConfig.RegistryEnvVar);
        if (!string.IsNullOrWhiteSpace(envRegistry))
        {
            Console.WriteLine($"环境变量 ({GlobalPackageConfig.RegistryEnvVar}): {envRegistry}");
        }

        if (projectConfig != null)
        {
            Console.WriteLine($"项目配置仓库: {projectConfig.GetEffectiveRegistryUrl() ?? "(未设置)"}");
        }

        Console.WriteLine($"API Key 状态: {(string.IsNullOrEmpty(effectiveApiKey) ? "未配置" : "已配置")}");
        return 0;
    }

    private static int ExecuteSet(string[] args)
    {
        var toProject = args.Contains("--project");
        var url = args.FirstOrDefault(a => !a.StartsWith('-'));

        if (string.IsNullOrWhiteSpace(url))
        {
            CommandHelper.PrintError("错误: 请提供远程 Old8Lang.PackageManager.Server 地址");
            Console.WriteLine("示例: old8lang registry set http://localhost:5000");
            return 1;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            CommandHelper.PrintError($"错误: 无效的 HTTP/HTTPS 地址: {url}");
            return 1;
        }

        var normalized = GlobalPackageConfig.NormalizeRegistryUrl(url);

        if (toProject)
        {
            var projectRoot = CommandHelper.FindProjectRoot();
            if (projectRoot == null)
            {
                CommandHelper.PrintError("错误: 当前目录不是 Old8Lang 项目（未找到 o8package.json）");
                return 1;
            }

            var config = ProjectConfig.LoadFromDirectory(projectRoot);
            if (config == null)
            {
                CommandHelper.PrintError("错误: 无法加载 o8package.json");
                return 1;
            }

            config.Registry = normalized;
            config.SaveToDirectory(projectRoot);
            CommandHelper.PrintSuccess($"已将项目包仓库地址设置为: {normalized}");
            return 0;
        }

        var globalConfig = GlobalPackageConfig.Load();
        globalConfig.Registry = normalized;
        globalConfig.Save();
        CommandHelper.PrintSuccess($"已将全局包仓库地址设置为: {normalized}");
        return 0;
    }

    private static int ExecuteReset(string[] args)
    {
        var toProject = args.Contains("--project");

        if (toProject)
        {
            var projectRoot = CommandHelper.FindProjectRoot();
            if (projectRoot == null)
            {
                CommandHelper.PrintError("错误: 当前目录不是 Old8Lang 项目");
                return 1;
            }

            var config = ProjectConfig.LoadFromDirectory(projectRoot);
            if (config == null)
            {
                CommandHelper.PrintError("错误: 无法加载 o8package.json");
                return 1;
            }

            config.Registry = null;
            config.SaveToDirectory(projectRoot);
            CommandHelper.PrintSuccess("已清除项目自定义仓库地址");
            return 0;
        }

        var globalConfig = GlobalPackageConfig.Load();
        globalConfig.Registry = null;
        globalConfig.Save();
        CommandHelper.PrintSuccess($"已重置全局包仓库为官方默认地址: {GlobalPackageConfig.DefaultRegistryUrl}");
        return 0;
    }

    private static int ExecuteTest(string[] args)
    {
        var urlOverride = args.FirstOrDefault(a => !a.StartsWith('-'));
        var projectRoot = CommandHelper.FindProjectRoot() ?? Directory.GetCurrentDirectory();
        var projectConfig = ProjectConfig.LoadFromDirectory(projectRoot);

        using var service = new PackageService(projectRoot, projectConfig, urlOverride);
        Console.WriteLine($"正在测试连接: {service.RegistryUrl}/v3/index.json ...");

        var ok = service.TestRegistryConnectionAsync().GetAwaiter().GetResult();
        if (ok)
        {
            CommandHelper.PrintSuccess($"✓ 连接成功 ({service.RegistryUrl})");
            return 0;
        }

        CommandHelper.PrintError($"✗ 无法连接到包仓库 ({service.RegistryUrl})");
        return 1;
    }

    private static int ExecuteSetKey(string[] args)
    {
        var apiKey = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            CommandHelper.PrintError("错误: 请提供 API Key");
            Console.WriteLine("用法: old8lang registry set-key <密钥>");
            return 1;
        }

        var globalConfig = GlobalPackageConfig.Load();
        globalConfig.ApiKey = apiKey.Trim();
        globalConfig.Save();
        CommandHelper.PrintSuccess($"已保存 API Key 到 {GlobalPackageConfig.GetDefaultConfigPath()}");
        return 0;
    }

    private static int ExecuteClearKey()
    {
        var globalConfig = GlobalPackageConfig.Load();
        globalConfig.ApiKey = null;
        globalConfig.Save();
        CommandHelper.PrintSuccess("已清除全局配置中的 API Key");
        return 0;
    }

    private static int UnknownSubCommand(string subCommand)
    {
        CommandHelper.PrintError($"错误: 未知子命令 '{subCommand}'");
        Console.WriteLine("使用 'old8lang registry -h' 查看帮助");
        return 1;
    }
}
