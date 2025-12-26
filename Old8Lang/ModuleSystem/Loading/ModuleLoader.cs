using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.ModuleSystem.Loading;

/// <summary>
/// 模块加载结果
/// </summary>
public class ModuleLoadResult
{
    /// <summary>
    /// 加载的代码块
    /// </summary>
    public BlockStatement? Block { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => Block != null && Error == null;

    /// <summary>
    /// 错误信息（如果加载失败）
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// 是否来自缓存
    /// </summary>
    public bool IsFromCache { get; set; }

    /// <summary>
    /// 模块绝对路径
    /// </summary>
    public string? AbsolutePath { get; set; }
}

/// <summary>
/// 模块加载器 - 负责加载和解析模块代码
/// </summary>
public class ModuleLoader
{
    private readonly CacheManager _cacheManager;

    public ModuleLoader(CacheManager? cacheManager = null)
    {
        _cacheManager = cacheManager ?? new CacheManager();
    }

    /// <summary>
    /// 加载模块
    /// </summary>
    /// <param name="modulePath">模块路径（绝对路径）</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="useCache">是否使用缓存</param>
    /// <returns>模块加载结果</returns>
    public ModuleLoadResult LoadModule(string modulePath, VariateManager manager, bool useCache = true)
    {
        var result = new ModuleLoadResult
        {
            AbsolutePath = modulePath
        };

        try
        {
            // 获取绝对路径
            var absolutePath = Path.GetFullPath(modulePath);
            result.AbsolutePath = absolutePath;

            // 1. 检查缓存
            if (useCache && _cacheManager.TryGetCached(absolutePath, out var cachedBlock))
            {
                result.Block = cachedBlock;
                result.IsFromCache = true;
                return result;
            }

            // 2. 加载文件内容
            if (!File.Exists(absolutePath))
            {
                result.Error = new FileNotFoundException($"模块文件不存在: {absolutePath}");
                return result;
            }

            var code = File.ReadAllText(absolutePath);

            // 3. 解析代码（不执行）
            var previousPath = manager.Path;
            manager.Path = absolutePath;

            try
            {
                var block = manager.Interpreter.Build(code: code);

                // 4. 缓存模块
                if (useCache)
                {
                    _cacheManager.AddToCache(absolutePath, block);
                }

                result.Block = block;
                result.IsFromCache = false;

                return result;
            }
            finally
            {
                manager.Path = previousPath;
            }
        }
        catch (Exception ex)
        {
            result.Error = ex;
            return result;
        }
    }

    /// <summary>
    /// 从目录加载模块（递归加载所有 .old8 文件）
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>模块加载结果</returns>
    public ModuleLoadResult LoadDirectory(string directoryPath, VariateManager manager)
    {
        var result = new ModuleLoadResult();

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                result.Error = new DirectoryNotFoundException($"目录不存在: {directoryPath}");
                return result;
            }

            // 查找入口文件
            var entryFile = FindDirectoryEntryFile(directoryPath);
            if (entryFile != null)
            {
                return LoadModule(entryFile, manager);
            }

            // 如果没有入口文件，尝试加载所有 .old8 文件
            var files = Directory.GetFiles(directoryPath, "*.old8", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                result.Error = new FileNotFoundException($"目录中没有找到 .old8 文件: {directoryPath}");
                return result;
            }

            // 合并所有文件内容
            var combinedCode = string.Join("\n", files.Select(File.ReadAllText));

            var block = manager.Interpreter.Build(code: combinedCode);
            result.Block = block;

            return result;
        }
        catch (Exception ex)
        {
            result.Error = ex;
            return result;
        }
    }

    /// <summary>
    /// 查找目录的入口文件
    /// </summary>
    private string? FindDirectoryEntryFile(string directoryPath)
    {
        var candidates = new[]
        {
            Path.Combine(directoryPath, "__init__.old8"),
            Path.Combine(directoryPath, "index.old8"),
            Path.Combine(directoryPath, "main.old8")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// 清除模块缓存
    /// </summary>
    /// <param name="modulePath">模块路径（可选，为空则清除所有缓存）</param>
    public void ClearCache(string? modulePath = null)
    {
        if (string.IsNullOrEmpty(modulePath))
        {
            _cacheManager.ClearAllCaches();
        }
        else
        {
            _cacheManager.ClearCache(modulePath);
        }
    }

    /// <summary>
    /// 获取缓存管理器
    /// </summary>
    public CacheManager CacheManager => _cacheManager;
}
