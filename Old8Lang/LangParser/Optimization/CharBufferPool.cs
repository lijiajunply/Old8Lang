using System.Buffers;

namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// 字符缓冲区池，用于减少字符数组分配
/// </summary>
/// <remarks>
/// 此类使用 ArrayPool&lt;char&gt; 管理字符缓冲区，
/// 减少 GC 压力并提升性能。
/// 使用 using 语句确保缓冲区自动归还。
/// </remarks>
public static class CharBufferPool
{
    private static readonly ArrayPool<char> Pool = ArrayPool<char>.Shared;

    /// <summary>
    /// 租用的缓冲区（实现 IDisposable）
    /// </summary>
    public struct RentedBuffer : IDisposable
    {
        private char[]? _buffer;
        private readonly int _length;

        internal RentedBuffer(char[] buffer, int length)
        {
            _buffer = buffer;
            _length = length;
        }

        /// <summary>
        /// 获取缓冲区的 Span 视图
        /// </summary>
        public Span<char> Span => _buffer != null ? _buffer.AsSpan(0, _length) : Span<char>.Empty;

        /// <summary>
        /// 归还缓冲区到池
        /// </summary>
        public void Dispose()
        {
            if (_buffer != null)
            {
                Pool.Return(_buffer);
                _buffer = null;
            }
        }
    }

    /// <summary>
    /// 租用字符缓冲区
    /// </summary>
    /// <param name="minimumLength">最小长度</param>
    /// <returns>租用的缓冲区（使用 using 语句自动归还）</returns>
    public static RentedBuffer Rent(int minimumLength)
    {
        var buffer = Pool.Rent(minimumLength);
        return new RentedBuffer(buffer, minimumLength);
    }
}
