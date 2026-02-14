using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// 环境管理命令
/// </summary>
public class EnvCommand : ICommand
{
    public string Name => "env";
    public string Description => "管理项目环境配置";
    public string Help => @"使用: Old8Lang.App env <子命令> [选项]

子命令:
  init              初始化环境配置文件
  list              列出所有可用环境
  show [env]        显示指定环境的配置（默认当前环境）
  set <key> <value> 设置环境变量
  get <key>         获取环境变量

选项:
  --env <name>      指定环境名称（默认: development）

示例:
  old8lang env init                    # 创建示例环境配置文件
  old8lang env list                    # 列出所有环境
  old8lang env show production         # 显示生产环境配置
  old8lang env set API_URL http://...  # 设置环境变量
  old8lang env get API_URL             # 获取环境变量
";

    public int Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 缺少子命令");
            Console.WriteLine(Help);
            return 1;
        }

        var subCommand = args[0].ToLower();

        try
        {
            return subCommand switch
            {
                "init" => InitEnvironment(args[1..]),
                "list" => ListEnvironments(args[1..]),
                "show" => ShowEnvironment(args[1..]),
                "set" => SetVariable(args[1..]),
                "get" => GetVariable(args[1..]),
                _ => throw new ArgumentException($"未知的子命令: {subCommand}")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            return 1;
        }
    }

    private int InitEnvironment(string[] args)
    {
        var projectRoot = ProjectConfig.FindProjectRoot(Directory.GetCurrentDirectory());

        if (projectRoot == null)
        {
            Console.WriteLine("错误: 未找到项目根目录（需要 o8package.json 文件）");
            Console.WriteLine("提示: 使用 'old8lang init <项目名>' 创建新项目");
            return 1;
        }

        Console.WriteLine($"在项目根目录创建环境配置文件: {projectRoot}");

        // 创建示例环境文件
        EnvironmentManager.CreateExampleEnvFiles(projectRoot);

        Console.WriteLine("✓ 已创建以下文件:");
        Console.WriteLine("  - .old8env (通用环境变量)");
        Console.WriteLine("  - .old8env.development (开发环境)");
        Console.WriteLine("  - .old8env.production (生产环境)");
        Console.WriteLine("  - .gitignore (环境文件忽略规则)");
        Console.WriteLine();
        Console.WriteLine("提示: 编辑这些文件以配置您的环境变量");

        return 0;
    }

    private int ListEnvironments(string[] args)
    {
        var projectRoot = ProjectConfig.FindProjectRoot(Directory.GetCurrentDirectory());

        if (projectRoot == null)
        {
            Console.WriteLine("错误: 未找到项目根目录（需要 o8package.json 文件）");
            return 1;
        }

        var projectConfig = ProjectConfig.LoadFromDirectory(projectRoot);
        if (projectConfig == null)
        {
            Console.WriteLine("错误: 无法加载项目配置");
            return 1;
        }

        var envManager = new EnvironmentManager(projectConfig);

        Console.WriteLine("可用环境:");
        Console.WriteLine();

        if (projectConfig.Old8Lang.Environments.Count == 0)
        {
            Console.WriteLine("  (未配置环境)");
            Console.WriteLine();
            Console.WriteLine("提示: 在 o8package.json 中添加环境配置:");
            Console.WriteLine(@"  ""old8lang"": {
    ""environments"": {
      ""development"": {
        ""debug"": true,
        ""logLevel"": ""debug""
      },
      ""production"": {
        ""debug"": false,
        ""logLevel"": ""error""
      }
    }
  }");
        }
        else
        {
            foreach (var (envName, envConfig) in projectConfig.Old8Lang.Environments)
            {
                var isCurrent = envName.Equals(envManager.CurrentEnvironment, StringComparison.OrdinalIgnoreCase);
                var marker = isCurrent ? "* " : "  ";

                Console.WriteLine($"{marker}{envName}");
                Console.WriteLine($"    运行时: {envConfig.Runtime ?? projectConfig.Old8Lang.Runtime}");
                Console.WriteLine($"    调试: {envConfig.Debug}");
                Console.WriteLine($"    日志级别: {envConfig.LogLevel}");

                if (envConfig.EnvironmentVariables.Count > 0)
                {
                    Console.WriteLine($"    环境变量: {envConfig.EnvironmentVariables.Count} 个");
                }

                Console.WriteLine();
            }

            Console.WriteLine($"当前环境: {envManager.CurrentEnvironment}");
        }

        return 0;
    }

    private int ShowEnvironment(string[] args)
    {
        var projectRoot = ProjectConfig.FindProjectRoot(Directory.GetCurrentDirectory());

        if (projectRoot == null)
        {
            Console.WriteLine("错误: 未找到项目根目录（需要 o8package.json 文件）");
            return 1;
        }

        var projectConfig = ProjectConfig.LoadFromDirectory(projectRoot);
        if (projectConfig == null)
        {
            Console.WriteLine("错误: 无法加载项目配置");
            return 1;
        }

        var environment = args.Length > 0 ? args[0] : null;
        var envManager = new EnvironmentManager(projectConfig, environment);

        Console.WriteLine($"环境: {envManager.CurrentEnvironment}");
        Console.WriteLine($"运行时模式: {envManager.GetRuntimeMode()}");
        Console.WriteLine($"调试模式: {envManager.GetDebugMode()}");
        Console.WriteLine($"日志级别: {envManager.GetLogLevel()}");
        Console.WriteLine();

        var variables = envManager.GetAllVariables();
        if (variables.Count > 0)
        {
            Console.WriteLine("环境变量:");
            foreach (var (key, value) in variables.OrderBy(kv => kv.Key))
            {
                // 隐藏敏感信息
                var displayValue = IsSensitiveKey(key) ? "********" : value;
                Console.WriteLine($"  {key}={displayValue}");
            }
        }
        else
        {
            Console.WriteLine("(未配置环境变量)");
        }

        return 0;
    }

    private int SetVariable(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("错误: 需要提供变量名和值");
            Console.WriteLine("使用: old8lang env set <key> <value>");
            return 1;
        }

        var key = args[0];
        var value = args[1];

        var projectRoot = ProjectConfig.FindProjectRoot(Directory.GetCurrentDirectory());

        if (projectRoot == null)
        {
            Console.WriteLine("错误: 未找到项目根目录（需要 o8package.json 文件）");
            return 1;
        }

        // 写入到 .old8env.local 文件（不应提交到版本控制）
        var envFilePath = Path.Combine(projectRoot, ".old8env.local");
        var lines = File.Exists(envFilePath) ? File.ReadAllLines(envFilePath).ToList() : new List<string>();

        // 查找并更新或添加变量
        var updated = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith($"{key}="))
            {
                lines[i] = $"{key}={value}";
                updated = true;
                break;
            }
        }

        if (!updated)
        {
            lines.Add($"{key}={value}");
        }

        File.WriteAllLines(envFilePath, lines);

        Console.WriteLine($"✓ 已设置环境变量: {key}={value}");
        Console.WriteLine($"  保存到: {envFilePath}");

        return 0;
    }

    private int GetVariable(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("错误: 需要提供变量名");
            Console.WriteLine("使用: old8lang env get <key>");
            return 1;
        }

        var key = args[0];

        var projectRoot = ProjectConfig.FindProjectRoot(Directory.GetCurrentDirectory());

        if (projectRoot == null)
        {
            Console.WriteLine("错误: 未找到项目根目录（需要 o8package.json 文件）");
            return 1;
        }

        var projectConfig = ProjectConfig.LoadFromDirectory(projectRoot);
        if (projectConfig == null)
        {
            Console.WriteLine("错误: 无法加载项目配置");
            return 1;
        }

        var envManager = new EnvironmentManager(projectConfig);
        var value = envManager.GetVariable(key);

        if (value != null)
        {
            Console.WriteLine(value);
            return 0;
        }
        else
        {
            Console.WriteLine($"错误: 环境变量 '{key}' 未定义");
            return 1;
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        var sensitivePatterns = new[] { "PASSWORD", "SECRET", "KEY", "TOKEN", "CREDENTIAL" };
        return sensitivePatterns.Any(pattern => key.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
