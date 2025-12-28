using System.Security.Cryptography;
using System.Text;

namespace Old8Lang.ModuleSystem.Loading;

/// <summary>
/// 网络模块加载器 - 负责从 URL 下载并缓存模块
/// </summary>
public class NetworkModuleLoader
{
    private static readonly HttpClient HttpClient = new();
    private readonly string CacheDirectory;

    public NetworkModuleLoader()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        CacheDirectory = Path.Combine(homeDir, ".old8lang", "cache", "network");

        // 确保缓存目录存在
        Directory.CreateDirectory(CacheDirectory);
    }

    /// <summary>
    /// 从 URL 下载模块（同步版本）
    /// </summary>
    /// <param name="url">模块 URL</param>
    /// <param name="forceDownload">是否强制重新下载（忽略缓存）</param>
    /// <returns>模块内容的本地缓存路径</returns>
    public string? DownloadModule(string url, bool forceDownload = false)
    {
        try
        {
            return DownloadModuleAsync(url, forceDownload).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从 URL 下载模块（异步版本）
    /// </summary>
    /// <param name="url">模块 URL</param>
    /// <param name="forceDownload">是否强制重新下载（忽略缓存）</param>
    /// <returns>模块内容的本地缓存路径</returns>
    public async Task<string?> DownloadModuleAsync(string url, bool forceDownload = false)
    {
        try
        {
            // 1. 计算 URL 哈希作为缓存键
            var urlHash = ComputeHash(url);
            var cachePath = Path.Combine(CacheDirectory, urlHash);
            var cacheFile = Path.Combine(cachePath, "module.old8");
            var metadataFile = Path.Combine(cachePath, "metadata.json");

            // 2. 检查缓存
            if (!forceDownload && File.Exists(cacheFile))
            {
                // 检查缓存是否过期（可选）
                var fileInfo = new FileInfo(cacheFile);
                var cacheAge = DateTime.Now - fileInfo.LastWriteTime;

                // 如果缓存小于 1 天，直接使用
                if (cacheAge.TotalDays < 1)
                {
                    return cacheFile;
                }
            }

            // 3. 下载模块内容
            Directory.CreateDirectory(cachePath);

            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // 4. 验证内容安全性
            if (!ValidateModuleContent(content))
            {
                throw new InvalidOperationException($"从 {url} 下载的模块内容验证失败");
            }

            // 5. 保存到缓存
            await File.WriteAllTextAsync(cacheFile, content);

            // 6. 保存元数据
            var metadata = new
            {
                url,
                downloadTime = DateTime.Now,
                contentLength = content.Length
            };
            await File.WriteAllTextAsync(metadataFile, System.Text.Json.JsonSerializer.Serialize(metadata));

            return cacheFile;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 计算字符串的 SHA256 哈希
    /// </summary>
    private string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    /// <summary>
    /// 验证模块内容的安全性
    /// </summary>
    /// <param name="content">模块内容</param>
    /// <returns>是否安全</returns>
    private bool ValidateModuleContent(string content)
    {
        // 基本验证：检查文件大小
        if (content.Length > 10 * 1024 * 1024) // 10MB 限制
        {
            return false;
        }

        // 可以添加更多验证规则：
        // - 检查是否包含可疑代码
        // - 语法检查
        // - 签名验证等

        return true;
    }

    /// <summary>
    /// 清除指定 URL 的缓存
    /// </summary>
    /// <param name="url">模块 URL</param>
    public void ClearCache(string url)
    {
        var urlHash = ComputeHash(url);
        var cachePath = Path.Combine(CacheDirectory, urlHash);

        if (Directory.Exists(cachePath))
        {
            Directory.Delete(cachePath, true);
        }
    }

    /// <summary>
    /// 清除所有网络模块缓存
    /// </summary>
    public void ClearAllCaches()
    {
        if (Directory.Exists(CacheDirectory))
        {
            Directory.Delete(CacheDirectory, true);
            Directory.CreateDirectory(CacheDirectory);
        }
    }

    /// <summary>
    /// 获取缓存的模块数量
    /// </summary>
    public int GetCachedModuleCount()
    {
        if (!Directory.Exists(CacheDirectory))
        {
            return 0;
        }

        return Directory.GetDirectories(CacheDirectory).Length;
    }
}