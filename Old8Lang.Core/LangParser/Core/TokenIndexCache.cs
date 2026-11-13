namespace Old8Lang.LangParser.Core;

/// <summary>
/// Token 索引缓存，用于优化 Token 查找性能
/// 提供按类型、按值快速查找 Token 索引的能力
/// </summary>
public class TokenIndexCache
{
    /// <summary>
    /// 按 Token 类型分组的索引
    /// Key: TokenType, Value: 该类型 Token 的索引列表
    /// </summary>
    private readonly Dictionary<LangTokenType, List<int>> _typeIndex;

    /// <summary>
    /// 按 Token 值分组的索引（用于快速查找特定标识符）
    /// Key: Token Value, Value: 该值 Token 的索引列表
    /// </summary>
    private readonly Dictionary<string, List<int>> _valueIndex;

    /// <summary>
    /// Token 列表引用
    /// </summary>
    private readonly List<LangToken> _tokens;

    /// <summary>
    /// 是否已构建索引
    /// </summary>
    private bool _isBuilt;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tokens">Token 列表</param>
    public TokenIndexCache(List<LangToken> tokens)
    {
        _tokens = tokens;
        _typeIndex = new Dictionary<LangTokenType, List<int>>();
        _valueIndex = new Dictionary<string, List<int>>();
        _isBuilt = false;
    }

    /// <summary>
    /// 构建索引（延迟初始化）
    /// </summary>
    public void BuildIndex()
    {
        if (_isBuilt) return;

        for (int i = 0; i < _tokens.Count; i++)
        {
            var token = _tokens[i];

            // 按类型索引
            if (!_typeIndex.TryGetValue(token.Type, out var typeList))
            {
                typeList = [];
                _typeIndex[token.Type] = typeList;
            }
            typeList.Add(i);

            // 按值索引（仅对标识符有意义）
            if (token.Type == LangTokenType.Identifier)
            {
                if (!_valueIndex.TryGetValue(token.Value, out var valueList))
                {
                    valueList = [];
                    _valueIndex[token.Value] = valueList;
                }
                valueList.Add(i);
            }
        }

        _isBuilt = true;
    }

    /// <summary>
    /// 在指定范围内查找下一个指定类型的 Token
    /// </summary>
    /// <param name="type">Token 类型</param>
    /// <param name="startIndex">开始索引</param>
    /// <param name="endIndex">结束索引（-1 表示到结尾）</param>
    /// <returns>Token 索引，未找到返回 -1</returns>
    public int FindNextToken(LangTokenType type, int startIndex, int endIndex = -1)
    {
        if (!_isBuilt) BuildIndex();

        if (!_typeIndex.TryGetValue(type, out var indices))
            return -1;

        int actualEndIndex = endIndex == -1 ? _tokens.Count - 1 : endIndex;

        // 二分查找第一个 >= startIndex 的位置
        int left = 0, right = indices.Count - 1;
        int result = -1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (indices[mid] >= startIndex)
            {
                if (indices[mid] <= actualEndIndex)
                {
                    result = indices[mid];
                }
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        return result;
    }

    /// <summary>
    /// 在指定范围内查找指定值的 Token
    /// </summary>
    /// <param name="value">Token 值</param>
    /// <param name="startIndex">开始索引</param>
    /// <param name="endIndex">结束索引（-1 表示到结尾）</param>
    /// <returns>Token 索引，未找到返回 -1</returns>
    public int FindTokenByValue(string value, int startIndex, int endIndex = -1)
    {
        if (!_isBuilt) BuildIndex();

        if (!_valueIndex.TryGetValue(value, out var indices))
            return -1;

        int actualEndIndex = endIndex == -1 ? _tokens.Count - 1 : endIndex;

        // 二分查找第一个 >= startIndex 的位置
        int left = 0, right = indices.Count - 1;
        int result = -1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (indices[mid] >= startIndex)
            {
                if (indices[mid] <= actualEndIndex)
                {
                    result = indices[mid];
                }
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定类型的所有 Token 索引
    /// </summary>
    /// <param name="type">Token 类型</param>
    /// <returns>Token 索引列表（只读）</returns>
    public IReadOnlyList<int> GetTokenIndicesByType(LangTokenType type)
    {
        if (!_isBuilt) BuildIndex();

        return _typeIndex.TryGetValue(type, out var indices)
            ? indices
            : Array.Empty<int>();
    }

    /// <summary>
    /// 统计指定范围内指定类型的 Token 数量
    /// </summary>
    /// <param name="type">Token 类型</param>
    /// <param name="startIndex">开始索引</param>
    /// <param name="endIndex">结束索引（-1 表示到结尾）</param>
    /// <returns>Token 数量</returns>
    public int CountTokens(LangTokenType type, int startIndex, int endIndex = -1)
    {
        if (!_isBuilt) BuildIndex();

        if (!_typeIndex.TryGetValue(type, out var indices))
            return 0;

        int actualEndIndex = endIndex == -1 ? _tokens.Count - 1 : endIndex;
        int count = 0;

        foreach (var index in indices)
        {
            if (index >= startIndex && index <= actualEndIndex)
                count++;
            else if (index > actualEndIndex)
                break;
        }

        return count;
    }
}
