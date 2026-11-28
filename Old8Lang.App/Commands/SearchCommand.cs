using Old8Lang.App.Services;
using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// old8lang search - 在远程 Old8Lang.PackageManager.Server 搜索包
/// </summary>
public class SearchCommand : ICommand
{
    public string Name => "search";
    public string Description => "在远程 Old8Lang 包仓库中搜索包";

    public string Help => @"
用法: old8lang search <关键词> [选项]

参数:
  <关键词>               搜索关键词（留空则列出热门/可用包）

选项:
  --prerelease           包含预发布版本
  -r, --registry <地址>  指定远程 Old8Lang.PackageManager.Server 地址
  -k, --api-key <密钥>   指定 API 密钥
  -h, --help             显示帮助信息

示例:
  old8lang search math
  old8lang search http --prerelease
  old8lang search utils --registry http://localhost:5000
";

    public int Execute(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Help);
            return 0;
        }

        var includePrerelease = args.Contains("--prerelease");
        var registryOverride = GetOptionValue(args, "-r", "--registry");
        var apiKeyOverride = GetOptionValue(args, "-k", "--api-key");
        var query = GetQuery(args) ?? string.Empty;

        var projectRoot = CommandHelper.FindProjectRoot() ?? Directory.GetCurrentDirectory();
        var config = ProjectConfig.LoadFromDirectory(projectRoot);

        try
        {
            using var service = new PackageService(projectRoot, config, registryOverride, apiKeyOverride);
            Console.WriteLine($"正在搜索 '{query}' (仓库: {service.RegistryUrl})...\n");

            var results = service.SearchPackagesAsync(query, includePrerelease).GetAwaiter().GetResult().ToList();
            if (results.Count == 0)
            {
                CommandHelper.PrintInfo("未找到匹配的包");
                return 0;
            }

            Console.WriteLine($"找到 {results.Count} 个包:\n");
            foreach (var pkg in results)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{pkg.Id}");
                Console.ResetColor();
                Console.WriteLine($" @{pkg.Version}");

                if (!string.IsNullOrWhiteSpace(pkg.Description))
                {
                    Console.WriteLine($"  {pkg.Description}");
                }

                if (!string.IsNullOrWhiteSpace(pkg.Author))
                {
                    Console.WriteLine($"  作者: {pkg.Author}");
                }

                Console.WriteLine();
            }

            return 0;
        }
        catch (Exception ex)
        {
            CommandHelper.PrintError($"搜索失败: {ex.Message}");
            return 1;
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

    private static string? GetQuery(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "-r" or "--registry" or "-k" or "--api-key")
            {
                i++;
                continue;
            }

            if (!args[i].StartsWith('-'))
            {
                return args[i];
            }
        }

        return null;
    }
}
