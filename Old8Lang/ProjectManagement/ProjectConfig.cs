using System.Text.Json;
using System.Text.Json.Serialization;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.ProjectManagement;

/// <summary>
/// Old8Lang 项目配置（类似 package.json）
/// 合并了 PackageConfiguration 的功能
/// </summary>
[Serializable]
public class ProjectConfig : PackageConfiguration
{
    /// <summary>
    /// 项目描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>
    /// 作者信息
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// 许可证
    /// </summary>
    [JsonPropertyName("license")]
    public string License { get; set; } = "MIT";

    /// <summary>
    /// Old8Lang 运行时配置
    /// </summary>
    [JsonPropertyName("old8lang")]
    public Old8LangConfig Old8Lang { get; set; } = new();

    /// <summary>
    /// 入口文件
    /// </summary>
    [JsonPropertyName("main")]
    public string? Main { get; set; }

    /// <summary>
    /// 脚本命令
    /// </summary>
    [JsonPropertyName("scripts")]
    public Dictionary<string, string> Scripts { get; set; } = new();
    
    /// <summary>
    /// 配置文件名
    /// </summary>
    [JsonIgnore]
    public const string FileName = "o8package.json";

    /// <summary>
    /// 从目录加载项目配置
    /// </summary>
    public static ProjectConfig? LoadFromDirectory(string directory)
    {
        var configPath = Path.Combine(directory, FileName);
        return LoadFromFile(configPath);
    }

    /// <summary>
    /// 从文件加载项目配置
    /// </summary>
    public static ProjectConfig? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Deserialize<ProjectConfig>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading project config from {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 保存项目配置到目录
    /// </summary>
    public void SaveToDirectory(string directory)
    {
        var configPath = Path.Combine(directory, FileName);
        SaveToFile(configPath);
    }

    /// <summary>
    /// 保存项目配置到文件
    /// </summary>
    public void SaveToFile(string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving project config to {filePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 向上查找项目根目录（包含 o8package.json）
    /// </summary>
    public static string? FindProjectRoot(string startPath, int maxDepth = 10)
    {
        var currentDir = Path.GetFullPath(startPath);

        for (int i = 0; i < maxDepth; i++)
        {
            var configPath = Path.Combine(currentDir, FileName);
            if (File.Exists(configPath))
            {
                return currentDir;
            }

            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (string.IsNullOrEmpty(parentDir) || parentDir == currentDir)
            {
                break;
            }

            currentDir = parentDir;
        }

        return null;
    }
}

/// <summary>
/// Old8Lang 运行时配置
/// </summary>
[Serializable]
public class Old8LangConfig
{
    /// <summary>
    /// Old8Lang 版本要求
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "^1.0.0";

    /// <summary>
    /// 运行时模式：interpreter 或 compiler
    /// </summary>
    [JsonPropertyName("runtime")]
    public string Runtime { get; set; } = "interpreter";
}