namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// 缓存统计信息
/// </summary>
/// <param name="TotalRequests">总请求次数</param>
/// <param name="CacheHits">缓存命中次数</param>
/// <param name="CacheMisses">缓存未命中次数</param>
/// <param name="CurrentSize">当前缓存大小</param>
/// <param name="HitRate">缓存命中率（0.0 - 1.0）</param>
public readonly record struct CacheStatistics(
    int TotalRequests,
    int CacheHits,
    int CacheMisses,
    int CurrentSize,
    double HitRate
);
