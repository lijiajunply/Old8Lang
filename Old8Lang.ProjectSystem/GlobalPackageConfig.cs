using System.Text.Json;
using System.Text.Json.Serialization;

namespace Old8Lang.ProjectManagement;

/// <summary>
/// Old8Lang 全局包管理配置（位于 ~/.old8lang/config.json）
/// 协议唯一绑定 Old8Lang.PackageManager.Server，支持切换自建私有服务器地址。
/// </summary>
[Serializable]
public class GlobalPackageConfig
{
    /// <summary>
    /// 官方默认远程包仓库地址
    /// </summary>
    public const string DefaultRegistryUrl = "https://package.old8lang.site";

    /// <summary>
    /// 覆盖远程包仓库地址的环境变量名
    /// </summary>
    public const string RegistryEnvVar = "OLD8LANG_REGISTRY";

    /// <summary>
    /// 覆盖 API Key 的环境变量名
    /// </summary>
    public const string ApiKeyEnvVar = "OLD8LANG_API_KEY";

    /// <summary>
    /// 默认远程 Old8Lang.PackageManager.Server 地址
    /// </summary>
    [JsonPropertyName("registry")]
    public string? Registry { get; set; }

    /// <summary>
    /// 远程包仓库发布/管理 API Key
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// 获取默认全局配置文件路径
    /// </summary>
    public static string GetDefaultConfigPath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".old8lang", "config.json");
    }

    /// <summary>
    /// 加载全局包管理配置
    /// </summary>
    public static GlobalPackageConfig Load(string? configPath = null)
    {
        var path = configPath ?? GetDefaultConfigPath();
        if (!File.Exists(path))
        {
            return new GlobalPackageConfig();
        }

        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<GlobalPackageConfig>(json, options) ?? new GlobalPackageConfig();
        }
        catch
        {
            return new GlobalPackageConfig();
        }
    }

    /// <summary>
    /// 保存全局包管理配置
    /// </summary>
    public void Save(string? configPath = null)
    {
        var path = configPath ?? GetDefaultConfigPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 按优先级解析唯一远程包仓库地址：
    /// CLI 参数 &gt; OLD8LANG_REGISTRY 环境变量 &gt; 项目 o8package.json &gt; ~/.old8lang/config.json &gt; 官方默认地址
    /// </summary>
    public static string ResolveRegistryUrl(
        string? cliRegistry = null,
        ProjectConfig? projectConfig = null,
        string? configPath = null)
    {
        if (!string.IsNullOrWhiteSpace(cliRegistry))
        {
            return NormalizeRegistryUrl(cliRegistry);
        }

        var envRegistry = Environment.GetEnvironmentVariable(RegistryEnvVar);
        if (!string.IsNullOrWhiteSpace(envRegistry))
        {
            return NormalizeRegistryUrl(envRegistry);
        }

        var projectRegistry = projectConfig?.GetEffectiveRegistryUrl();
        if (!string.IsNullOrWhiteSpace(projectRegistry))
        {
            return NormalizeRegistryUrl(projectRegistry);
        }

        var globalConfig = Load(configPath);
        if (!string.IsNullOrWhiteSpace(globalConfig.Registry))
        {
            return NormalizeRegistryUrl(globalConfig.Registry);
        }

        return DefaultRegistryUrl;
    }

    /// <summary>
    /// 按优先级解析 API Key：
    /// CLI 参数 &gt; OLD8LANG_API_KEY 环境变量 &gt; ~/.old8lang/config.json
    /// </summary>
    public static string? ResolveApiKey(string? cliApiKey = null, string? configPath = null)
    {
        if (!string.IsNullOrWhiteSpace(cliApiKey))
        {
            return cliApiKey.Trim();
        }

        var envApiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            return envApiKey.Trim();
        }

        var globalConfig = Load(configPath);
        return string.IsNullOrWhiteSpace(globalConfig.ApiKey) ? null : globalConfig.ApiKey.Trim();
    }

    /// <summary>
    /// 规范化远程包仓库地址（去掉末尾 / 及末尾重复的 /v3）
    /// </summary>
    public static string NormalizeRegistryUrl(string url)
    {
        var normalized = url.Trim().TrimEnd('/');
        if (normalized.EndsWith("/v3", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^3].TrimEnd('/');
        }

        return normalized;
    }
}
