using System.Text.RegularExpressions;

namespace Old8Lang.ModuleSystem.Resolution;

/// <summary>
/// 版本解析器 - 负责解析和匹配模块版本
/// </summary>
public partial class VersionResolver
{
    [GeneratedRegex(@"^(\d+)\.(\d+)\.(\d+)$")]
    private static partial Regex VersionRegex();

    [GeneratedRegex(@"^(.+?)@(.+)$")]
    private static partial Regex PackageVersionRegex();

    /// <summary>
    /// 解析包名和版本
    /// </summary>
    /// <param name="packageSpec">包规范（如 "package@1.0.0" 或 "package"）</param>
    /// <param name="packageName">输出：包名</param>
    /// <param name="versionSpec">输出：版本规范</param>
    public void ParsePackageSpec(string packageSpec, out string packageName, out string? versionSpec)
    {
        var match = PackageVersionRegex().Match(packageSpec);
        if (match.Success)
        {
            packageName = match.Groups[1].Value;
            versionSpec = match.Groups[2].Value;
        }
        else
        {
            packageName = packageSpec;
            versionSpec = null;
        }
    }

    /// <summary>
    /// 检查版本是否满足规范
    /// </summary>
    /// <param name="version">实际版本（如 "1.2.3"）</param>
    /// <param name="versionSpec">版本规范（如 "^1.0.0", ">=2.0.0", "1.2.3"）</param>
    /// <returns>是否满足版本要求</returns>
    public bool IsVersionMatch(string version, string versionSpec)
    {
        if (string.IsNullOrEmpty(versionSpec))
        {
            return true; // 无版本要求，任何版本都匹配
        }

        var parsedVersion = ParseVersion(version);
        if (parsedVersion == null)
        {
            return false;
        }

        // 处理不同的版本规范
        if (versionSpec.StartsWith("^"))
        {
            // 兼容版本（主版本相同）
            var requiredVersion = ParseVersion(versionSpec[1..]);
            return requiredVersion != null &&
                   parsedVersion.Major == requiredVersion.Major &&
                   parsedVersion >= requiredVersion;
        }

        if (versionSpec.StartsWith("~"))
        {
            // 次版本兼容（主版本和次版本相同）
            var requiredVersion = ParseVersion(versionSpec[1..]);
            return requiredVersion != null &&
                   parsedVersion.Major == requiredVersion.Major &&
                   parsedVersion.Minor == requiredVersion.Minor &&
                   parsedVersion >= requiredVersion;
        }

        if (versionSpec.StartsWith(">="))
        {
            var requiredVersion = ParseVersion(versionSpec[2..].Trim());
            return requiredVersion != null && parsedVersion >= requiredVersion;
        }

        if (versionSpec.StartsWith("<="))
        {
            var requiredVersion = ParseVersion(versionSpec[2..].Trim());
            return requiredVersion != null && parsedVersion <= requiredVersion;
        }

        if (versionSpec.StartsWith(">"))
        {
            var requiredVersion = ParseVersion(versionSpec[1..].Trim());
            return requiredVersion != null && parsedVersion > requiredVersion;
        }

        if (versionSpec.StartsWith("<"))
        {
            var requiredVersion = ParseVersion(versionSpec[1..].Trim());
            return requiredVersion != null && parsedVersion < requiredVersion;
        }

        // 精确版本匹配
        var exactVersion = ParseVersion(versionSpec);
        return exactVersion != null && parsedVersion == exactVersion;
    }

    /// <summary>
    /// 解析版本字符串
    /// </summary>
    /// <param name="versionString">版本字符串（如 "1.2.3"）</param>
    /// <returns>版本对象，解析失败返回 null</returns>
    private SemanticVersion? ParseVersion(string versionString)
    {
        if (string.IsNullOrEmpty(versionString))
        {
            return null;
        }

        var match = VersionRegex().Match(versionString.Trim());
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups[1].Value, out var major) ||
            !int.TryParse(match.Groups[2].Value, out var minor) ||
            !int.TryParse(match.Groups[3].Value, out var patch))
        {
            return null;
        }

        return new SemanticVersion(major, minor, patch);
    }

    /// <summary>
    /// 从版本化目录列表中选择最佳匹配版本
    /// </summary>
    /// <param name="directories">目录列表（如 ["package@1.0.0", "package@1.2.0", "package@2.0.0"]）</param>
    /// <param name="versionSpec">版本规范</param>
    /// <returns>最佳匹配的目录名，如果没有匹配返回 null</returns>
    public string? SelectBestVersion(IEnumerable<string> directories, string? versionSpec)
    {
        var matchedDirs = new List<(string dir, SemanticVersion version)>();

        foreach (var dir in directories)
        {
            // 从目录名中提取版本
            var atIndex = dir.LastIndexOf('@');
            if (atIndex < 0)
            {
                continue;
            }

            var versionString = dir[(atIndex + 1)..];
            var version = ParseVersion(versionString);
            if (version == null)
            {
                continue;
            }

            // 检查是否满足版本要求
            if (string.IsNullOrEmpty(versionSpec) || IsVersionMatch(versionString, versionSpec))
            {
                matchedDirs.Add((dir, version));
            }
        }

        // 返回最高版本
        return matchedDirs
            .OrderByDescending(x => x.version)
            .FirstOrDefault().dir;
    }
}

/// <summary>
/// 语义版本类
/// </summary>
public class SemanticVersion(int major, int minor, int patch)
    : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    public int Major { get; } = major;
    public int Minor { get; } = minor;
    public int Patch { get; } = patch;

    public int CompareTo(SemanticVersion? other)
    {
        if (other == null)
        {
            return 1;
        }

        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        return Patch.CompareTo(other.Patch);
    }

    public bool Equals(SemanticVersion? other)
    {
        return other != null &&
               Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SemanticVersion);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch);
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right)
    {
        return !Equals(left, right);
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(SemanticVersion left, SemanticVersion right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(SemanticVersion left, SemanticVersion right)
    {
        return left.CompareTo(right) <= 0;
    }
}