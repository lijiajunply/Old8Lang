using System;
using System.Collections.Generic;

namespace Old8Lang.Interpreter;

/// <summary>
/// 变量缓存，使用 LRU 策略缓存变量查找结果
/// </summary>
public class VariableCache
{
    private readonly int _maxSize;
    private readonly Dictionary<string, CacheEntry> _cache;
    private readonly LinkedList<string> _lruList;

    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public int HitCount { get; private set; }

    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    public int MissCount { get; private set; }

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double HitRate => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) : 0;

    /// <summary>
    /// 当前缓存大小
    /// </summary>
    public int Count => _cache.Count;

    public VariableCache(int maxSize = 1000)
    {
        if (maxSize <= 0)
        {
            throw new ArgumentException("缓存大小必须大于0", nameof(maxSize));
        }

        _maxSize = maxSize;
        _cache = new Dictionary<string, CacheEntry>(maxSize);
        _lruList = new LinkedList<string>();
    }

    /// <summary>
    /// 尝试从缓存获取变量值
    /// </summary>
    public bool TryGet(string variableName, out object? value)
    {
        if (_cache.TryGetValue(variableName, out var entry))
        {
            // 命中缓存
            HitCount++;
            entry.AccessCount++;
            entry.LastAccessTime = DateTime.Now;

            // 更新 LRU 列表
            _lruList.Remove(entry.ListNode!);
            _lruList.AddFirst(entry.ListNode);

            value = entry.Value;
            return true;
        }

        // 未命中缓存
        MissCount++;
        value = null;
        return false;
    }

    /// <summary>
    /// 添加或更新缓存条目
    /// </summary>
    public void Set(string variableName, object? value, int scopeLevel = 0)
    {
        if (_cache.TryGetValue(variableName, out var existingEntry))
        {
            // 更新现有条目
            existingEntry.Value = value;
            existingEntry.LastAccessTime = DateTime.Now;
            existingEntry.IsDirty = false;

            // 更新 LRU 列表
            _lruList.Remove(existingEntry.ListNode!);
            _lruList.AddFirst(existingEntry.ListNode);
        }
        else
        {
            // 检查缓存是否已满
            if (_cache.Count >= _maxSize)
            {
                // 移除最少使用的条目
                EvictLeastRecentlyUsed();
            }

            // 添加新条目
            var listNode = _lruList.AddFirst(variableName);
            var entry = new CacheEntry
            {
                VariableName = variableName,
                ScopeLevel = scopeLevel,
                Value = value,
                LastAccessTime = DateTime.Now,
                AccessCount = 0,
                IsDirty = false,
                ListNode = listNode
            };

            _cache[variableName] = entry;
        }
    }

    /// <summary>
    /// 标记变量为已修改
    /// </summary>
    public void MarkDirty(string variableName)
    {
        if (_cache.TryGetValue(variableName, out var entry))
        {
            entry.IsDirty = true;
        }
    }

    /// <summary>
    /// 移除缓存条目
    /// </summary>
    public void Remove(string variableName)
    {
        if (_cache.TryGetValue(variableName, out var entry))
        {
            _lruList.Remove(entry.ListNode!);
            _cache.Remove(variableName);
        }
    }

    /// <summary>
    /// 清除指定作用域级别的所有缓存
    /// </summary>
    public void ClearScope(int scopeLevel)
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in _cache)
        {
            if (kvp.Value.ScopeLevel >= scopeLevel)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        _lruList.Clear();
        HitCount = 0;
        MissCount = 0;
    }

    /// <summary>
    /// 移除最少使用的条目（LRU 淘汰）
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        if (_lruList.Last != null)
        {
            var lruKey = _lruList.Last.Value;
            _cache.Remove(lruKey);
            _lruList.RemoveLast();
        }
    }

    /// <summary>
    /// 缓存条目
    /// </summary>
    private class CacheEntry
    {
        public string VariableName { get; set; } = string.Empty;
        public int ScopeLevel { get; set; }
        public object? Value { get; set; }
        public DateTime LastAccessTime { get; set; }
        public int AccessCount { get; set; }
        public bool IsDirty { get; set; }
        public LinkedListNode<string>? ListNode { get; set; }
    }
}
