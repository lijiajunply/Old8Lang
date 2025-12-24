using System.Text.Json;
using System.Text.Json.Serialization;

namespace Old8Lang.ProjectManagement;

/// <summary>
/// 锁文件（类似 package-lock.json）
/// </summary>
[Serializable]
public class LockFile
{
    /// <summary>
    /// 锁文件版本
    /// </summary>
    [JsonPropertyName("lockfileVersion")]
    public int LockfileVersion { get; set; } = 1;

    /// <summary>
    /// 生成时间
    /// </summary>
    [JsonPropertyName("generated")]
    public DateTime Generated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Old8Lang 版本信息
    /// </summary>
    [JsonPropertyName("old8lang")]
    public Old8LangLockInfo Old8Lang { get; set; } = new();

    /// <summary>
    /// 锁定的包信息
    /// </summary>
    [JsonPropertyName("packages")]
    public Dictionary<string, PackageLockInfo> Packages { get; set; } = new();

    /// <summary>
    /// 锁文件名
    /// </summary>
    public const string FileName = "old8.lock.json";

    /// <summary>
    /// 从目录加载锁文件
    /// </summary>
    public static LockFile? LoadFromDirectory(string directory)
    {
        var lockPath = Path.Combine(directory, FileName);
        return LoadFromFile(lockPath);
    }

    /// <summary>
    /// 从文件加载锁文件
    /// </summary>
    public static LockFile? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            return JsonSerializer.Deserialize<LockFile>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading lock file from {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 保存锁文件到目录
    /// </summary>
    public void SaveToDirectory(string directory)
    {
        var lockPath = Path.Combine(directory, FileName);
        SaveToFile(lockPath);
    }

    /// <summary>
    /// 保存锁文件到文件
    /// </summary>
    public void SaveToFile(string filePath)
    {
        try
        {
            // 更新生成时间
            Generated = DateTime.UtcNow;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving lock file to {filePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 生成锁文件（根据项目配置和已安装的包）
    /// </summary>
    public static LockFile Generate(ProjectConfig config, string packagesDirectory)
    {
        var lockFile = new LockFile
        {
            Old8Lang = new Old8LangLockInfo
            {
                Version = config.Old8Lang.Version
            }
        };

        // 扫描已安装的包
        if (Directory.Exists(packagesDirectory))
        {
            foreach (var packageDir in Directory.GetDirectories(packagesDirectory))
            {
                var dirName = Path.GetFileName(packageDir);

                // 解析包名和版本: PackageName@version
                var parts = dirName.Split('@');
                if (parts.Length != 2)
                    continue;

                var packageName = parts[0];
                var version = parts[1];

                // 读取 package.json
                var packageJsonPath = Path.Combine(packageDir, "package.json");
                if (!File.Exists(packageJsonPath))
                    continue;

                try
                {
                    var packageJson = File.ReadAllText(packageJsonPath);
                    var packageInfo = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(packageJson);

                    var lockInfo = new PackageLockInfo
                    {
                        Version = version,
                        Resolved = $"local:{packageDir}",
                        Dependencies = new Dictionary<string, string>()
                    };

                    // 提取依赖信息
                    if (packageInfo != null && packageInfo.TryGetValue("dependencies", out var deps))
                    {
                        if (deps.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in deps.EnumerateObject())
                            {
                                lockInfo.Dependencies[prop.Name] = prop.Value.GetString() ?? "*";
                            }
                        }
                    }

                    lockFile.Packages[packageName] = lockInfo;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to read package info for {packageName}: {ex.Message}");
                }
            }
        }

        return lockFile;
    }
}

/// <summary>
/// Old8Lang 锁定信息
/// </summary>
[Serializable]
public class Old8LangLockInfo
{
    /// <summary>
    /// 锁定的 Old8Lang 版本
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Old8Lang 二进制文件的哈希值（可选）
    /// </summary>
    [JsonPropertyName("hash")]
    public string? Hash { get; set; }
}

/// <summary>
/// 包锁定信息
/// </summary>
[Serializable]
public class PackageLockInfo
{
    /// <summary>
    /// 精确版本
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    /// <summary>
    /// 包的来源 URL 或路径
    /// </summary>
    [JsonPropertyName("resolved")]
    public string Resolved { get; set; } = "";

    /// <summary>
    /// 包的完整性哈希值（SHA256）
    /// </summary>
    [JsonPropertyName("integrity")]
    public string? Integrity { get; set; }

    /// <summary>
    /// 依赖的包及版本范围
    /// </summary>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new();
}