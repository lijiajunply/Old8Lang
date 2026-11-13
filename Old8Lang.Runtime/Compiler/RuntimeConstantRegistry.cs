using System.Collections.Concurrent;

namespace Old8Lang.Compiler;

/// <summary>
/// 编译期注册、运行期取用的常量表。
/// </summary>
/// <remarks>
/// 用于把「编译期就已经存在的对象」（例如扩展方法包装器、
/// 已被解释器预执行注册好的函数值）带进生成的 IL：
/// 生成代码只嵌入整数 id，运行时通过 <see cref="Get"/> 取回对象。
/// </remarks>
public static class RuntimeConstantRegistry
{
    private static readonly ConcurrentDictionary<int, object> Values = new();
    private static int _nextId;

    /// <summary>
    /// 注册一个常量对象。
    /// </summary>
    /// <returns>常量 id，供生成代码通过 <see cref="Get"/> 取回</returns>
    public static int Register(object value)
    {
        var id = Interlocked.Increment(ref _nextId);
        Values[id] = value;
        return id;
    }

    /// <summary>
    /// 取回已注册的常量对象。
    /// </summary>
    public static object Get(int id)
    {
        if (!Values.TryGetValue(id, out var value))
        {
            throw new InvalidOperationException($"常量 '{id}' 未注册");
        }

        return value;
    }
}
