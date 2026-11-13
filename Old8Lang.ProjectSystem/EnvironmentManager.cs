using System.Text.Json;

namespace Old8Lang.ProjectManagement;

/// <summary>
/// 环境管理器 - 管理开发/生产环境配置
/// </summary>
public class EnvironmentManager
{
    /// <summary>
    /// 当前环境名称
    /// </summary>
    public string CurrentEnvironment { get; private set; }

    /// <summary>
    /// 项目配置
    /// </summary>
    private readonly ProjectConfig _projectConfig;

    /// <summary>
    /// 环境变量缓存
    /// </summary>
    private readonly Dictionary<string, string> _environmentVariables = new();

    /// <summary>
    /// 环境文件名
    /// </summary>
    private const string EnvFileName = ".old8env";

    /// <summary>
    /// 构造函数
    /// </summary>
    public EnvironmentManager(ProjectConfig projectConfig, string? environment = null)
    {
        _projectConfig = projectConfig;

        // 从环境变量或参数确定当前环境
        CurrentEnvironment = environment
            ?? Environment.GetEnvironmentVariable("OLD8_ENV")
            ?? "development";

        LoadEnvironmentVariables();
    }

    /// <summary>
    /// 加载环境变量
    /// </summary>
    private void LoadEnvironmentVariables()
    {
        // 1. 加载通用环境变量（从 .old8env 文件）
        LoadEnvFile(EnvFileName);

        // 2. 加载环境特定的变量（从 .old8env.{environment} 文件）
        LoadEnvFile($"{EnvFileName}.{CurrentEnvironment}");

        // 3. 加载项目配置中的环境变量
        if (_projectConfig.Old8Lang.Environments.TryGetValue(CurrentEnvironment, out var envConfig))
        {
            foreach (var (key, value) in envConfig.EnvironmentVariables)
            {
                _environmentVariables[key] = value;
            }
        }

        // 4. 系统环境变量优先级最高
        foreach (var key in _environmentVariables.Keys.ToList())
        {
            var sysValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(sysValue))
            {
                _environmentVariables[key] = sysValue;
            }
        }
    }

    /// <summary>
    /// 从文件加载环境变量
    /// </summary>
    private void LoadEnvFile(string fileName)
    {
        var projectRoot = ProjectConfig.FindProjectRoot(Directory.GetCurrentDirectory());
        if (projectRoot == null) return;

        var envFilePath = Path.Combine(projectRoot, fileName);
        if (!File.Exists(envFilePath)) return;

        try
        {
            var lines = File.ReadAllLines(envFilePath);
            foreach (var line in lines)
            {
                // 跳过注释和空行
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                // 解析 KEY=VALUE 格式
                var parts = trimmed.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    // 移除引号
                    if ((value.StartsWith('"') && value.EndsWith('"')) ||
                        (value.StartsWith('\'') && value.EndsWith('\'')))
                    {
                        value = value[1..^1];
                    }

                    _environmentVariables[key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load {fileName}: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取环境变量
    /// </summary>
    public string? GetVariable(string key)
    {
        return _environmentVariables.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// 设置环境变量
    /// </summary>
    public void SetVariable(string key, string value)
    {
        _environmentVariables[key] = value;
    }

    /// <summary>
    /// 获取所有环境变量
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAllVariables()
    {
        return _environmentVariables;
    }

    /// <summary>
    /// 获取当前环境配置
    /// </summary>
    public EnvironmentConfig? GetCurrentEnvironmentConfig()
    {
        return _projectConfig.Old8Lang.Environments.TryGetValue(CurrentEnvironment, out var config)
            ? config
            : null;
    }

    /// <summary>
    /// 是否为开发环境
    /// </summary>
    public bool IsDevelopment => CurrentEnvironment.Equals("development", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 是否为生产环境
    /// </summary>
    public bool IsProduction => CurrentEnvironment.Equals("production", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 获取运行时模式（考虑环境覆盖）
    /// </summary>
    public string GetRuntimeMode()
    {
        var envConfig = GetCurrentEnvironmentConfig();
        return envConfig?.Runtime ?? _projectConfig.Old8Lang.Runtime;
    }

    /// <summary>
    /// 获取调试模式
    /// </summary>
    public bool GetDebugMode()
    {
        var envConfig = GetCurrentEnvironmentConfig();
        return envConfig?.Debug ?? IsDevelopment;
    }

    /// <summary>
    /// 获取日志级别
    /// </summary>
    public string GetLogLevel()
    {
        var envConfig = GetCurrentEnvironmentConfig();
        return envConfig?.LogLevel ?? (IsDevelopment ? "debug" : "info");
    }

    /// <summary>
    /// 获取环境特定的脚本
    /// </summary>
    public Dictionary<string, string> GetEnvironmentScripts()
    {
        var envConfig = GetCurrentEnvironmentConfig();
        var scripts = new Dictionary<string, string>(_projectConfig.Scripts);

        // 合并环境特定的脚本
        if (envConfig?.Scripts != null)
        {
            foreach (var (key, value) in envConfig.Scripts)
            {
                scripts[key] = value;
            }
        }

        return scripts;
    }

    /// <summary>
    /// 创建示例环境配置
    /// </summary>
    public static void CreateExampleEnvFiles(string projectRoot)
    {
        // 创建 .old8env 文件
        var envContent = @"# Old8Lang 环境变量配置
# 通用环境变量（所有环境共享）

# 应用配置
APP_NAME=MyOld8App
APP_VERSION=1.0.0

# 数据库配置（开发环境默认值）
DB_HOST=localhost
DB_PORT=5432
DB_NAME=old8lang_dev
";
        File.WriteAllText(Path.Combine(projectRoot, EnvFileName), envContent);

        // 创建 .old8env.development 文件
        var devEnvContent = @"# 开发环境特定配置
DEBUG=true
LOG_LEVEL=debug
API_URL=http://localhost:3000
";
        File.WriteAllText(Path.Combine(projectRoot, $"{EnvFileName}.development"), devEnvContent);

        // 创建 .old8env.production 文件
        var prodEnvContent = @"# 生产环境特定配置
DEBUG=false
LOG_LEVEL=error
API_URL=https://api.production.com
";
        File.WriteAllText(Path.Combine(projectRoot, $"{EnvFileName}.production"), prodEnvContent);

        // 创建 .gitignore 条目提示
        var gitignorePath = Path.Combine(projectRoot, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            File.WriteAllText(gitignorePath, @"# Old8Lang 环境文件
.old8env.local
.old8env.*.local
");
        }
    }
}
