namespace Old8Lang.ProjectManagement;

/// <summary>
/// 虚拟环境管理器
/// 负责检测和管理项目级别的包隔离环境
/// </summary>
public class VirtualEnvironment
{
    /// <summary>
    /// 项目根目录
    /// </summary>
    public string ProjectRoot { get; }

    /// <summary>
    /// 项目配置
    /// </summary>
    public ProjectConfig Config { get; }

    /// <summary>
    /// 锁文件（可能不存在）
    /// </summary>
    public LockFile? LockFile { get; }

    /// <summary>
    /// 是否启用虚拟环境
    /// </summary>
    public bool IsEnabled => Config.PackageManager.UseVirtualEnv;

    /// <summary>
    /// 包目录的绝对路径
    /// </summary>
    public string PackagesDirectory { get; }

    /// <summary>
    /// 调试日志开关
    /// </summary>
    public static bool DebugEnabled { get; set; }

    private VirtualEnvironment(string projectRoot, ProjectConfig config, LockFile? lockFile)
    {
        ProjectRoot = projectRoot;
        Config = config;
        LockFile = lockFile;

        // 计算包目录的绝对路径
        var packagesDir = config.PackageManager.PackagesDir;
        if (Path.IsPathRooted(packagesDir))
        {
            PackagesDirectory = packagesDir;
        }
        else
        {
            PackagesDirectory = Path.GetFullPath(Path.Combine(projectRoot, packagesDir));
        }

        LogDebug($"Virtual environment detected:");
        LogDebug($"  Project root: {ProjectRoot}");
        LogDebug($"  Project name: {Config.Name} v{Config.Version}");
        LogDebug($"  Packages dir: {PackagesDirectory}");
        LogDebug($"  Virtual env enabled: {IsEnabled}");
        LogDebug($"  Lock file: {(LockFile != null ? "present" : "missing")}");
    }

    /// <summary>
    /// 从指定路径开始检测虚拟环境
    /// </summary>
    /// <param name="startPath">开始检测的路径（文件或目录）</param>
    /// <returns>检测到的虚拟环境，如果未检测到则返回 null</returns>
    public static VirtualEnvironment? Detect(string? startPath)
    {
        if (string.IsNullOrEmpty(startPath))
            return null;

        // 如果是文件，获取其目录
        var searchDir = File.Exists(startPath)
            ? Path.GetDirectoryName(startPath)
            : startPath;

        if (string.IsNullOrEmpty(searchDir))
            return null;

        // 向上查找项目根目录
        var projectRoot = ProjectConfig.FindProjectRoot(searchDir);
        if (projectRoot == null)
        {
            LogDebug("No project root found (no o8packages.json)");
            return null;
        }

        // 加载项目配置
        var config = ProjectConfig.LoadFromDirectory(projectRoot);
        if (config == null)
        {
            LogDebug($"Failed to load project config from {projectRoot}");
            return null;
        }

        // 加载锁文件（可选）
        var lockFile = LockFile.LoadFromDirectory(projectRoot);

        return new VirtualEnvironment(projectRoot, config, lockFile);
    }

    /// <summary>
    /// 获取包搜索路径列表（按优先级排序）
    /// </summary>
    public List<string> GetPackageSearchPaths()
    {
        var paths = new List<string>();

        if (IsEnabled)
        {
            // 优先级 1: 项目本地包目录
            if (Directory.Exists(PackagesDirectory))
            {
                paths.Add(PackagesDirectory);
                LogDebug($"Added virtual env packages path: {PackagesDirectory}");
            }
        }

        return paths;
    }

    /// <summary>
    /// 解析包名到具体的包路径
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <returns>包的完整路径，如果未找到则返回 null</returns>
    public string? ResolvePackage(string packageName)
    {
        if (!IsEnabled || !Directory.Exists(PackagesDirectory))
            return null;

        // 如果有锁文件，使用锁定的版本
        if (LockFile != null && LockFile.Packages.TryGetValue(packageName, out var lockInfo))
        {
            var lockedVersion = lockInfo.Version;
            var lockedPath = Path.Combine(PackagesDirectory, $"{packageName}@{lockedVersion}");

            if (Directory.Exists(lockedPath))
            {
                LogDebug($"Resolved {packageName} to locked version {lockedVersion}: {lockedPath}");
                return lockedPath;
            }

            LogDebug($"Warning: Locked version {lockedVersion} not found for {packageName}");

            if (Config.PackageManager.Strict)
            {
                throw new InvalidOperationException(
                    $"Strict mode: Package '{packageName}' version {lockedVersion} not found. " +
                    $"Run 'old8lang install' to fix.");
            }
        }

        // 如果有配置的版本要求，尝试匹配
        var versionRange = Config.Dependencies.TryGetValue(packageName, out var dependency)
            ? dependency
            : Config.DevDependencies.GetValueOrDefault(packageName);

        if (versionRange != null)
        {
            var matchedPath = FindPackageByVersionRange(packageName, versionRange);
            if (matchedPath != null)
            {
                LogDebug($"Resolved {packageName} to version range {versionRange}: {matchedPath}");
                return matchedPath;
            }
        }

        // 回退：查找任意版本
        var packageDirs = Directory.GetDirectories(PackagesDirectory, $"{packageName}@*");
        if (packageDirs.Length > 0)
        {
            // 返回第一个找到的（未来可以实现版本排序）
            var selectedPath = packageDirs[0];
            LogDebug($"Resolved {packageName} to any version: {selectedPath}");
            return selectedPath;
        }

        LogDebug($"Package {packageName} not found in virtual environment");
        return null;
    }

    /// <summary>
    /// 根据版本范围查找包
    /// </summary>
    private string? FindPackageByVersionRange(string packageName, string versionRange)
    {
        if (!Directory.Exists(PackagesDirectory))
            return null;

        // 简化版本匹配（未来可以实现完整的语义版本匹配）
        var packageDirs = Directory.GetDirectories(PackagesDirectory, $"{packageName}@*");

        foreach (var dir in packageDirs)
        {
            var dirName = Path.GetFileName(dir);
            var parts = dirName.Split('@');
            if (parts.Length == 2)
            {
                var version = parts[1];

                // 简单匹配逻辑
                if (VersionMatches(version, versionRange))
                {
                    return dir;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 简单的版本匹配（未来需要完整的语义版本实现）
    /// </summary>
    private bool VersionMatches(string version, string range)
    {
        // 精确匹配
        if (range == version)
            return true;

        // 任意版本
        if (range == "*")
            return true;

        // ^ 兼容版本（简化实现）
        if (range.StartsWith("^"))
        {
            var baseVersion = range.Substring(1);
            return version.StartsWith(baseVersion.Split('.')[0]);
        }

        // ~ 补丁版本（简化实现）
        if (range.StartsWith("~"))
        {
            var baseVersion = range.Substring(1);
            var baseParts = baseVersion.Split('.');
            var versionParts = version.Split('.');

            if (baseParts.Length >= 2 && versionParts.Length >= 2)
            {
                return baseParts[0] == versionParts[0] && baseParts[1] == versionParts[1];
            }
        }

        return false;
    }

    /// <summary>
    /// 初始化虚拟环境（创建必要的目录和文件）
    /// </summary>
    public void Initialize()
    {
        // 创建包目录
        if (!Directory.Exists(PackagesDirectory))
        {
            Directory.CreateDirectory(PackagesDirectory);
            LogDebug($"Created packages directory: {PackagesDirectory}");
        }

        // 如果没有锁文件且启用了自动锁定，生成锁文件
        if (LockFile == null && Config.PackageManager.AutoLock)
        {
            var newLockFile = LockFile.Generate(Config, PackagesDirectory);
            newLockFile.SaveToDirectory(ProjectRoot);
            LogDebug($"Generated lock file: {Path.Combine(ProjectRoot, LockFile.FileName)}");
        }
    }

    private static void LogDebug(string message)
    {
        if (DebugEnabled)
        {
            Console.WriteLine($"[VirtualEnvironment] {message}");
        }
    }
}

/// <summary>
/// 已安装包的信息
/// </summary>
[Serializable]
public class InstalledPackageInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Path { get; set; } = "";
}