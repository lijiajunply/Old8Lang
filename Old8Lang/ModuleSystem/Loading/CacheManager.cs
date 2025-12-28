using Old8Lang.AST.Statement;

namespace Old8Lang.ModuleSystem.Loading;

/// <summary>
/// 模块缓存管理器 - 负责管理已加载模块的缓存
/// </summary>
public class CacheManager
{
    private readonly Dictionary<string, BlockStatement> ModuleCache = new();
    private readonly Lock CacheLock = new();

    /// <summary>
    /// 尝试从缓存获取模块
    /// </summary>
    /// <param name="modulePath">模块绝对路径</param>
    /// <param name="cachedBlock">缓存的模块代码块</param>
    /// <returns>是否存在缓存</returns>
    public bool TryGetCached(string modulePath, out BlockStatement? cachedBlock)
    {
        lock (CacheLock)
        {
            return ModuleCache.TryGetValue(modulePath, out cachedBlock);
        }
    }

    /// <summary>
    /// 添加模块到缓存
    /// </summary>
    /// <param name="modulePath">模块绝对路径</param>
    /// <param name="block">模块代码块</param>
    public void AddToCache(string modulePath, BlockStatement block)
    {
        lock (CacheLock)
        {
            ModuleCache[modulePath] = block;
        }
    }

    /// <summary>
    /// 清除指定模块的缓存
    /// </summary>
    /// <param name="modulePath">模块绝对路径</param>
    /// <returns>是否成功清除</returns>
    public bool ClearCache(string modulePath)
    {
        lock (CacheLock)
        {
            return ModuleCache.Remove(modulePath);
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAllCaches()
    {
        lock (CacheLock)
        {
            ModuleCache.Clear();
        }
    }

    /// <summary>
    /// 获取缓存的模块数量
    /// </summary>
    public int CachedModuleCount
    {
        get
        {
            lock (CacheLock)
            {
                return ModuleCache.Count;
            }
        }
    }

    /// <summary>
    /// 检查模块是否已缓存
    /// </summary>
    /// <param name="modulePath">模块绝对路径</param>
    /// <returns>是否已缓存</returns>
    public bool IsCached(string modulePath)
    {
        lock (CacheLock)
        {
            return ModuleCache.ContainsKey(modulePath);
        }
    }

    /// <summary>
    /// 获取所有已缓存的模块路径
    /// </summary>
    /// <returns>模块路径列表</returns>
    public IEnumerable<string> GetCachedModulePaths()
    {
        lock (CacheLock)
        {
            return ModuleCache.Keys.ToList();
        }
    }
}
