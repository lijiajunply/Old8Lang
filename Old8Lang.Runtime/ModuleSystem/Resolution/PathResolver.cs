namespace Old8Lang.ModuleSystem.Resolution;

/// <summary>
/// 路径解析器 - 负责规范化和解析模块路径
/// 修复了原有的路径解析 HACK 和冗余代码
/// </summary>
public class PathResolver
{
    /// <summary>
    /// 解析模块路径为绝对路径
    /// </summary>
    /// <param name="modulePath">模块路径（可以是相对路径或绝对路径）</param>
    /// <param name="currentFilePath">当前文件的路径</param>
    /// <returns>解析后的绝对路径</returns>
    public string ResolvePath(string modulePath, string? currentFilePath)
    {
        if (string.IsNullOrEmpty(modulePath))
        {
            throw new ArgumentException("模块路径不能为空", nameof(modulePath));
        }

        // 1. 标准化路径
        var normalized = NormalizePath(modulePath);

        // 2. 处理绝对路径
        if (Path.IsPathRooted(normalized))
        {
            return Path.GetFullPath(normalized);
        }

        // 3. 处理相对路径
        return ResolveRelativePath(normalized, currentFilePath);
    }

    /// <summary>
    /// 标准化路径格式
    /// </summary>
    /// <param name="path">原始路径</param>
    /// <returns>标准化后的路径</returns>
    private string NormalizePath(string path)
    {
        // 移除引号
        path = path.Trim('"', '\'');

        // 统一路径分隔符
        path = path.Replace('\\', Path.DirectorySeparatorChar);

        // 修复 macOS/Linux 路径（移除原有的 HACK）
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            // 修复缺少前导斜杠的绝对路径
            if ((path.StartsWith("Users/") || path.StartsWith("home/") ||
                 path.StartsWith("Volumes/") || path.StartsWith("mnt/")) &&
                !path.StartsWith("/"))
            {
                path = "/" + path;
            }
        }

        return path;
    }

    /// <summary>
    /// 解析相对路径
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <param name="currentFilePath">当前文件路径或基础目录路径</param>
    /// <returns>绝对路径</returns>
    private string ResolveRelativePath(string relativePath, string? currentFilePath)
    {
        string baseDirectory;

        if (!string.IsNullOrEmpty(currentFilePath))
        {
            if (Directory.Exists(currentFilePath))
            {
                baseDirectory = Path.GetFullPath(currentFilePath);
            }
            else
            {
                var directory = Path.GetDirectoryName(currentFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    baseDirectory = Path.GetFullPath(directory);
                }
                else
                {
                    var fullPath = Path.GetFullPath(currentFilePath);
                    baseDirectory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
                }
            }
        }
        else
        {
            baseDirectory = Directory.GetCurrentDirectory();
        }

        var combinedPath = Path.Combine(baseDirectory, relativePath);
        return Path.GetFullPath(combinedPath);
    }

    /// <summary>
    /// 检查路径是否为相对路径
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>是否为相对路径</returns>
    public bool IsRelativePath(string path)
    {
        return !Path.IsPathRooted(path) &&
               !path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
               !path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查路径是否为 URL
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>是否为 URL</returns>
    public bool IsUrl(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 添加文件扩展名（如果缺少）
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="defaultExtension">默认扩展名（默认为 .old8）</param>
    /// <returns>带扩展名的路径</returns>
    public string EnsureExtension(string path, string defaultExtension = ".old8")
    {
        var ext = Path.GetExtension(path).ToLower();

        // 如果已经有支持的扩展名，直接返回
        if (ext == ".old8" || ext == ".ol")
        {
            return path;
        }

        // 如果是 URL，不添加扩展名
        if (IsUrl(path))
        {
            return path;
        }

        // 添加默认扩展名
        return path + defaultExtension;
    }

    /// <summary>
    /// 验证路径安全性（防止路径遍历攻击）
    /// </summary>
    /// <param name="path">要验证的路径</param>
    /// <param name="allowedBasePath">允许的基础路径</param>
    /// <returns>路径是否安全</returns>
    public bool IsPathSafe(string path, string? allowedBasePath = null)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);

            // 检查路径遍历攻击
            if (fullPath.Contains(".."))
            {
                return false;
            }

            // 如果指定了允许的基础路径，检查是否在允许范围内
            if (!string.IsNullOrEmpty(allowedBasePath))
            {
                var baseFullPath = Path.GetFullPath(allowedBasePath);
                if (!fullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
