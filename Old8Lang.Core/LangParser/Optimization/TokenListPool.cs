using Microsoft.Extensions.ObjectPool;

namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// Token 列表池，用于减少列表分配
/// </summary>
/// <remarks>
/// 使用 ObjectPool 管理 List&lt;LangToken&gt; 实例，
/// 减少 GC 压力并提升性能。
/// </remarks>
public static class TokenListPool
{
    private static readonly ObjectPool<List<LangToken>> Pool =
        new DefaultObjectPool<List<LangToken>>(new TokenListPolicy(), 50);

    private class TokenListPolicy : IPooledObjectPolicy<List<LangToken>>
    {
        public List<LangToken> Create() => new List<LangToken>(128);

        public bool Return(List<LangToken> obj)
        {
            if (obj.Count > 10000) // 不池化过大的列表
            {
                return false;
            }

            obj.Clear();
            return true;
        }
    }

    /// <summary>
    /// 租用 Token 列表
    /// </summary>
    /// <returns>已清空的 Token 列表</returns>
    public static List<LangToken> Rent()
    {
        var list = Pool.Get();
        list.Clear(); // 确保列表为空
        return list;
    }

    /// <summary>
    /// 归还 Token 列表到池
    /// </summary>
    /// <param name="list">要归还的列表</param>
    public static void Return(List<LangToken> list)
    {
        Pool.Return(list);
    }
}
