using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Models;

/// <summary>
/// 按行建立的 Token 索引结构，用于将位置查找（行列 -> Token / 索引）与行范围切片
/// 从 O(N) 线性扫描降至 O(1) 行定位 + O(log K) 行内二分。
/// </summary>
public sealed class TokenLineIndex
{
    /// <summary>单行在原始 token 列表中的连续切片信息（仅当行号单调递增时可用）</summary>
    public readonly record struct LineSlice(int StartIndex, int Count);

    private readonly IReadOnlyList<LangToken> _tokens;
    private readonly LineSlice[] _denseLines;
    private readonly int _maxLine; // 1-based 最大行号
    private readonly Dictionary<int, List<int>>? _nonMonotonicFallback;

    /// <summary>空的索引单例</summary>
    public static TokenLineIndex Empty { get; } = new([]);

    /// <summary>被索引的原始 Token 数量</summary>
    public int Count => _tokens.Count;

    /// <summary>是否采用紧凑单调数组索引（正常词法输出恒为 true）</summary>
    public bool IsMonotonic => _nonMonotonicFallback == null;

    /// <summary>最大 1-based 行号（无 token 时为 0）</summary>
    public int MaxLine => _maxLine;

    public TokenLineIndex(IReadOnlyList<LangToken> tokens)
    {
        _tokens = tokens ?? [];
        if (_tokens.Count == 0)
        {
            _denseLines = [];
            _maxLine = 0;
            return;
        }

        // 检查行号是否单调不减且为正整数，同时统计最大行号
        var maxLine = 0;
        var prevLine = 0;
        var monotonic = true;

        for (var i = 0; i < _tokens.Count; i++)
        {
            var line = _tokens[i].Line;
            if (line < 1 || line < prevLine)
            {
                monotonic = false;
            }
            if (line > maxLine)
            {
                maxLine = line;
            }
            prevLine = line;
        }

        _maxLine = maxLine;

        if (monotonic && maxLine > 0 && maxLine <= 1_000_000)
        {
            var slices = new LineSlice[maxLine + 1]; // 1-based 索引
            var i = 0;
            while (i < _tokens.Count)
            {
                var line = _tokens[i].Line;
                var start = i;
                while (i < _tokens.Count && _tokens[i].Line == line)
                {
                    i++;
                }
                slices[line] = new LineSlice(start, i - start);
            }

            _denseLines = slices;
            return;
        }

        // 兜底：手工构造的非单调或超大行号 token 列表
        _denseLines = [];
        _nonMonotonicFallback = new Dictionary<int, List<int>>();
        for (var i = 0; i < _tokens.Count; i++)
        {
            var line = _tokens[i].Line;
            if (!_nonMonotonicFallback.TryGetValue(line, out var indices))
            {
                indices = [];
                _nonMonotonicFallback[line] = indices;
            }
            indices.Add(i);
        }
    }

    /// <summary>
    /// 获取指定 LSP 0-based 行在原始 token 列表中的连续范围（仅单调模式有效；空行返回 Count == 0）。
    /// </summary>
    public LineSlice GetLineSlice(int lspLine)
    {
        var tokenLine = lspLine + 1;
        if (tokenLine < 1 || tokenLine >= _denseLines.Length)
        {
            return default;
        }

        return _denseLines[tokenLine];
    }

    /// <summary>
    /// 获取指定 LSP 0-based 行的所有 token 下标。
    /// </summary>
    public IEnumerable<int> EnumerateLineTokenIndices(int lspLine)
    {
        var tokenLine = lspLine + 1;
        if (_nonMonotonicFallback != null)
        {
            if (_nonMonotonicFallback.TryGetValue(tokenLine, out var indices))
            {
                foreach (var idx in indices)
                {
                    yield return idx;
                }
            }
            yield break;
        }

        if (tokenLine < 1 || tokenLine >= _denseLines.Length)
        {
            yield break;
        }

        var slice = _denseLines[tokenLine];
        for (var i = 0; i < slice.Count; i++)
        {
            yield return slice.StartIndex + i;
        }
    }

    /// <summary>
    /// 根据 LSP 0-based 行列查找命中的 Token 及其在列表中的索引。
    /// </summary>
    public (LangToken Token, int Index)? FindTokenAtPosition(int lspLine, int lspColumn)
    {
        var tokenLine = lspLine + 1;

        if (_nonMonotonicFallback != null)
        {
            if (_nonMonotonicFallback.TryGetValue(tokenLine, out var indices))
            {
                foreach (var idx in indices)
                {
                    var token = _tokens[idx];
                    if (ContainsColumn(token, lspColumn))
                    {
                        return (token, idx);
                    }
                }
            }
            return null;
        }

        if (tokenLine < 1 || tokenLine >= _denseLines.Length)
        {
            return null;
        }

        var slice = _denseLines[tokenLine];
        if (slice.Count == 0)
        {
            return null;
        }

        // 行内二分定位：token.Column 正常情况下单调不减
        var lo = slice.StartIndex;
        var hi = slice.StartIndex + slice.Count - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var token = _tokens[mid];
            var endCol = GetTokenEndColumn(token);

            if (lspColumn < token.Column)
            {
                hi = mid - 1;
            }
            else if (lspColumn > endCol)
            {
                lo = mid + 1;
            }
            else
            {
                return (token, mid);
            }
        }

        // 若同一行存在手工构造的乱序列号，退回该行内小范围扫描（仅限单行 K 个 token）
        for (var i = slice.StartIndex; i < slice.StartIndex + slice.Count; i++)
        {
            var token = _tokens[i];
            if (ContainsColumn(token, lspColumn))
            {
                return (token, i);
            }
        }

        return null;
    }

    /// <summary>
    /// 按精确起始位置（LSP 0-based 行、列）查找 token 索引。
    /// </summary>
    public int FindTokenIndexAtExactStart(int lspLine, int lspColumn)
    {
        var tokenLine = lspLine + 1;

        if (_nonMonotonicFallback != null)
        {
            if (_nonMonotonicFallback.TryGetValue(tokenLine, out var indices))
            {
                foreach (var idx in indices)
                {
                    if (_tokens[idx].Column == lspColumn)
                    {
                        return idx;
                    }
                }
            }
            return -1;
        }

        if (tokenLine < 1 || tokenLine >= _denseLines.Length)
        {
            return -1;
        }

        var slice = _denseLines[tokenLine];
        var lo = slice.StartIndex;
        var hi = slice.StartIndex + slice.Count - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var col = _tokens[mid].Column;
            if (col == lspColumn)
            {
                // 若存在多个同列起始 token，返回第一个
                while (mid > slice.StartIndex && _tokens[mid - 1].Column == lspColumn)
                {
                    mid--;
                }
                return mid;
            }

            if (col < lspColumn)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// 查找位于或早于指定 LSP 0-based (line, column) 的最后一个 token 索引；不存在时返回 -1。
    /// 用于成员链分析（向前回溯点号）和签名帮助（向前回溯左括号），避免从文件末尾线性倒扫。
    /// </summary>
    public int FindLastTokenAtOrBefore(int lspLine, int lspColumn)
    {
        if (_tokens.Count == 0)
        {
            return -1;
        }

        var targetLine = lspLine + 1;
        if (targetLine < 1)
        {
            return -1;
        }

        if (_nonMonotonicFallback != null)
        {
            for (var i = _tokens.Count - 1; i >= 0; i--)
            {
                var token = _tokens[i];
                if (token.Line < targetLine || (token.Line == targetLine && token.Column <= lspColumn))
                {
                    return i;
                }
            }
            return -1;
        }

        // 若目标行超过最后一行，直接返回最后一个 token
        if (targetLine >= _denseLines.Length)
        {
            return _tokens.Count - 1;
        }

        // 先在目标行内寻找最后一个 Column <= lspColumn 的 token
        var slice = _denseLines[targetLine];
        if (slice.Count > 0)
        {
            var best = -1;
            var lo = slice.StartIndex;
            var hi = slice.StartIndex + slice.Count - 1;
            while (lo <= hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                if (_tokens[mid].Column <= lspColumn)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (best >= 0)
            {
                return best;
            }
        }

        // 目标行没有满足条件的 token，向前找到最近的非空行的最后一个 token
        for (var line = targetLine - 1; line >= 1; line--)
        {
            var prevSlice = _denseLines[line];
            if (prevSlice.Count > 0)
            {
                return prevSlice.StartIndex + prevSlice.Count - 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// 获取覆盖 LSP 0-based 闭区间 [startLspLine, endLspLine] 的连续 token 下标范围 [Start, EndExclusive)。
    /// 若范围内无 token，返回 (0, 0)。
    /// </summary>
    public (int Start, int EndExclusive) GetTokenRangeForLines(int startLspLine, int endLspLine)
    {
        if (_tokens.Count == 0 || endLspLine < startLspLine)
        {
            return (0, 0);
        }

        if (_nonMonotonicFallback != null)
        {
            return (0, _tokens.Count);
        }

        var startLine = Math.Max(1, startLspLine + 1);
        var endLine = Math.Min(_denseLines.Length - 1, endLspLine + 1);
        if (startLine > endLine)
        {
            return (0, 0);
        }

        var firstIndex = -1;
        for (var line = startLine; line <= endLine; line++)
        {
            if (_denseLines[line].Count > 0)
            {
                firstIndex = _denseLines[line].StartIndex;
                break;
            }
        }

        if (firstIndex < 0)
        {
            return (0, 0);
        }

        var lastExclusive = firstIndex;
        for (var line = endLine; line >= startLine; line--)
        {
            var slice = _denseLines[line];
            if (slice.Count > 0)
            {
                lastExclusive = slice.StartIndex + slice.Count;
                break;
            }
        }

        return (firstIndex, lastExclusive);
    }

    private static int GetTokenEndColumn(LangToken token) =>
        token.Value.Length > 0 ? token.Column + token.Value.Length - 1 : token.Column;

    private static bool ContainsColumn(LangToken token, int column) =>
        column >= token.Column && column <= GetTokenEndColumn(token);
}
