using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Interpreter;

/// <summary>
/// 符号表缓存，用于优化变量查找性能
/// 提供快速的变量名查找和作用域解析
/// </summary>
public class SymbolTableCache
{
    /// <summary>
    /// 符号查找缓存
    /// Key: 变量名，Value: (作用域索引, 缓存时间戳)
    /// </summary>
    private readonly Dictionary<string, (int ScopeIndex, long Timestamp)> _symbolLookupCache = new();

    /// <summary>
    /// 函数签名缓存
    /// Key: 函数名，Value: 函数参数列表签名
    /// </summary>
    private readonly Dictionary<string, string> _functionSignatureCache = new();

    /// <summary>
    /// 类成员缓存
    /// Key: "类名.成员名"，Value: 成员类型
    /// </summary>
    private readonly Dictionary<string, string> _classMemberCache = new();

    /// <summary>
    /// 缓存版本号（用于失效检测）
    /// 每次作用域发生变化时递增
    /// </summary>
    private long _cacheVersion = 0;

    /// <summary>
    /// 最大缓存版本差（超过此值则认为缓存过期）
    /// </summary>
    private const long MaxCacheVersionDiff = 100;

    /// <summary>
    /// 是否启用缓存（默认启用）
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// 增加缓存版本号（在作用域变化时调用）
    /// </summary>
    public void InvalidateCache()
    {
        _cacheVersion++;

        // 如果版本号增长过大，清理过期缓存
        if (_cacheVersion % MaxCacheVersionDiff == 0)
        {
            CleanupExpiredCache();
        }
    }

    /// <summary>
    /// 尝试从缓存中获取符号的作用域索引
    /// </summary>
    /// <param name="symbolName">符号名</param>
    /// <param name="scopeIndex">输出作用域索引</param>
    /// <returns>如果缓存命中返回 true，否则返回 false</returns>
    public bool TryGetSymbolScope(string symbolName, out int scopeIndex)
    {
        scopeIndex = -1;

        if (!EnableCache)
            return false;

        if (_symbolLookupCache.TryGetValue(symbolName, out var cached))
        {
            // 检查缓存是否过期
            if (_cacheVersion - cached.Timestamp <= MaxCacheVersionDiff)
            {
                scopeIndex = cached.ScopeIndex;
                return true;
            }
            else
            {
                // 缓存过期，移除
                _symbolLookupCache.Remove(symbolName);
            }
        }

        return false;
    }

    /// <summary>
    /// 缓存符号的作用域索引
    /// </summary>
    /// <param name="symbolName">符号名</param>
    /// <param name="scopeIndex">作用域索引</param>
    public void CacheSymbolScope(string symbolName, int scopeIndex)
    {
        if (!EnableCache)
            return;

        _symbolLookupCache[symbolName] = (scopeIndex, _cacheVersion);
    }

    /// <summary>
    /// 缓存函数签名
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="signature">函数签名（参数类型）</param>
    public void CacheFunctionSignature(string functionName, string signature)
    {
        if (!EnableCache)
            return;

        _functionSignatureCache[functionName] = signature;
    }

    /// <summary>
    /// 尝试获取函数签名
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="signature">输出函数签名</param>
    /// <returns>如果缓存命中返回 true，否则返回 false</returns>
    public bool TryGetFunctionSignature(string functionName, out string signature)
    {
        signature = string.Empty;

        if (!EnableCache)
            return false;

        return _functionSignatureCache.TryGetValue(functionName, out signature!);
    }

    /// <summary>
    /// 缓存类成员信息
    /// </summary>
    /// <param name="className">类名</param>
    /// <param name="memberName">成员名</param>
    /// <param name="memberType">成员类型</param>
    public void CacheClassMember(string className, string memberName, string memberType)
    {
        if (!EnableCache)
            return;

        var key = $"{className}.{memberName}";
        _classMemberCache[key] = memberType;
    }

    /// <summary>
    /// 尝试获取类成员类型
    /// </summary>
    /// <param name="className">类名</param>
    /// <param name="memberName">成员名</param>
    /// <param name="memberType">输出成员类型</param>
    /// <returns>如果缓存命中返回 true，否则返回 false</returns>
    public bool TryGetClassMember(string className, string memberName, out string memberType)
    {
        memberType = string.Empty;

        if (!EnableCache)
            return false;

        var key = $"{className}.{memberName}";
        return _classMemberCache.TryGetValue(key, out memberType!);
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    private void CleanupExpiredCache()
    {
        var expiredKeys = _symbolLookupCache
            .Where(kvp => _cacheVersion - kvp.Value.Timestamp > MaxCacheVersionDiff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _symbolLookupCache.Remove(key);
        }
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void ClearCache()
    {
        _symbolLookupCache.Clear();
        _functionSignatureCache.Clear();
        _classMemberCache.Clear();
        _cacheVersion = 0;
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计字符串</returns>
    public string GetCacheStats()
    {
        return $"符号查找缓存: {_symbolLookupCache.Count}, " +
               $"函数签名缓存: {_functionSignatureCache.Count}, " +
               $"类成员缓存: {_classMemberCache.Count}, " +
               $"缓存版本: {_cacheVersion}";
    }
}
