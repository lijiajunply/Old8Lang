using System.Collections.Concurrent;

namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// 字符串缓存，用于减少重复字符串的内存分配
/// </summary>
/// <remarks>
/// 此类使用 ConcurrentDictionary 缓存短字符串（<= 64 字符），
/// 以减少解析过程中的内存分配和 GC 压力。
/// 线程安全，可在多线程环境中使用。
/// </remarks>
public sealed class StringCache
{
    private readonly ConcurrentDictionary<string, string> _cache;
    private readonly int _maxCacheSize;
    private int _currentSize;
    private int _totalRequests;
    private int _cacheHits;
    private int _cacheMisses;

    /// <summary>
    /// 创建字符串缓存实例
    /// </summary>
    /// <param name="maxCacheSize">最大缓存大小（默认 10000）</param>
    public StringCache(int maxCacheSize = 10000)
    {
        _cache = new ConcurrentDictionary<string, string>();
        _maxCacheSize = maxCacheSize;
        _currentSize = 0;
        _totalRequests = 0;
        _cacheHits = 0;
        _cacheMisses = 0;
    }

    /// <summary>
    /// 获取或添加字符串到缓存
    /// </summary>
    /// <param name="value">字符串值（Span）</param>
    /// <returns>缓存的字符串</returns>
    /// <remarks>
    /// 只缓存长度 <= 64 的字符串。
    /// 如果缓存已满，新字符串将不被缓存。
    /// </remarks>
    public string GetOrAdd(ReadOnlySpan<char> value)
    {
        Interlocked.Increment(ref _totalRequests);

        // 只缓存短字符串（<= 64 字符）
        if (value.Length > 64)
        {
            Interlocked.Increment(ref _cacheMisses);
            return new string(value);
        }

        var key = new string(value);

        if (_cache.TryGetValue(key, out var cached))
        {
            Interlocked.Increment(ref _cacheHits);
            return cached;
        }

        // 检查缓存大小限制（在添加之前）
        if (_currentSize >= _maxCacheSize)
        {
            // 缓存已满，不再添加
            Interlocked.Increment(ref _cacheMisses);
            return key;
        }

        // 尝试添加到缓存（使用 TryAdd 避免重复添加）
        if (_cache.TryAdd(key, key))
        {
            // 成功添加，增加计数
            Interlocked.Increment(ref _currentSize);
            Interlocked.Increment(ref _cacheMisses);
            return key;
        }

        // 并发情况下，其他线程已经添加了相同的键
        Interlocked.Increment(ref _cacheHits);
        return _cache[key];
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        _currentSize = 0;
        _totalRequests = 0;
        _cacheHits = 0;
        _cacheMisses = 0;
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计信息</returns>
    public CacheStatistics GetStatistics()
    {
        var totalRequests = _totalRequests;
        var cacheHits = _cacheHits;
        var cacheMisses = _cacheMisses;
        var currentSize = _currentSize;

        var hitRate = totalRequests > 0 ? (double)cacheHits / totalRequests : 0.0;

        return new CacheStatistics(
            TotalRequests: totalRequests,
            CacheHits: cacheHits,
            CacheMisses: cacheMisses,
            CurrentSize: currentSize,
            HitRate: hitRate
        );
    }
}
