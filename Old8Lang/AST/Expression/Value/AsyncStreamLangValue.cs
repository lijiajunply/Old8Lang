using Old8Lang.AST;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 异步流值类型
/// 表示一个异步数据流，可以通过异步迭代器消费
/// </summary>
public class AsyncStreamLangValue : LangValueType
{
    private readonly IAsyncEnumerable<LangValueType> _asyncEnumerable;
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncStreamLangValue(IAsyncEnumerable<LangValueType> asyncEnumerable, CancellationToken cancellationToken = default, SourcePosition position = default)
        : base(position)
    {
        _asyncEnumerable = asyncEnumerable;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// 获取异步枚举器
    /// </summary>
    public IAsyncEnumerator<LangValueType> GetAsyncEnumerator()
    {
        return _asyncEnumerable.GetAsyncEnumerator(_cancellationToken);
    }

    /// <summary>
    /// 异步流映射操作
    /// </summary>
    public AsyncStreamLangValue Map(Func<LangValueType, LangValueType> mapper, SourcePosition position = default)
    {
        var mappedEnumerable = _asyncEnumerable.Select(mapper);
        return new AsyncStreamLangValue(mappedEnumerable, _cancellationToken, position);
    }

    /// <summary>
    /// 异步流过滤操作
    /// </summary>
    public AsyncStreamLangValue Filter(Func<LangValueType, bool> predicate, SourcePosition position = default)
    {
        var filteredEnumerable = _asyncEnumerable.Where(predicate);
        return new AsyncStreamLangValue(filteredEnumerable, _cancellationToken, position);
    }

    /// <summary>
    /// 异步流归约操作
    /// </summary>
    public async Task<LangValueType> Reduce(Func<LangValueType, LangValueType, LangValueType> reducer, LangValueType initialValue, SourcePosition position = default)
    {
        var result = initialValue;
        await foreach (var item in _asyncEnumerable.WithCancellation(_cancellationToken))
        {
            result = reducer(result, item);
        }
        return result;
    }

    /// <summary>
    /// 将异步流转换为列表
    /// </summary>
    public async Task<ListLangValue> ToList(SourcePosition position = default)
    {
        var list = new List<LangValueType>();
        await foreach (var item in _asyncEnumerable.WithCancellation(_cancellationToken))
        {
            list.Add(item);
        }
        return new ListLangValue(list, position);
    }

    /// <summary>
    /// 获取底层 IAsyncEnumerable 对象
    /// </summary>
    public override object GetValue() => _asyncEnumerable;

    /// <summary>
    /// 类型字符串表示
    /// </summary>
    public override string TypeToString() => "AsyncStream";

    /// <summary>
    /// 值的字符串表示
    /// </summary>
    public override string ToString()
    {
        return "AsyncStream";
    }

    /// <summary>
    /// Run 方法：返回自身
    /// </summary>
    public override LangValueType Run(VariateManager manager) => this;

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(
            Position,
            "编译模式暂不支持 AsyncStream 类型"
        );
    }

    /// <summary>
    /// 获取 .NET 类型（编译器模式暂不支持）
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        return typeof(IAsyncEnumerable<object>);
    }

    /// <summary>
    /// 创建空的异步流
    /// </summary>
    public static AsyncStreamLangValue Empty(SourcePosition position = default)
    {
        return new AsyncStreamLangValue(AsyncEnumerable.Empty<LangValueType>(), default, position);
    }

    /// <summary>
    /// 从可枚举对象创建异步流
    /// </summary>
    public static AsyncStreamLangValue FromEnumerable(IEnumerable<LangValueType> enumerable, SourcePosition position = default)
    {
        return new AsyncStreamLangValue(enumerable.ToAsyncEnumerable(), default, position);
    }

    /// <summary>
    /// 创建范围异步流
    /// </summary>
    public static AsyncStreamLangValue Range(int start, int count, SourcePosition position = default)
    {
        return new AsyncStreamLangValue(
            AsyncEnumerable.Range(start, count).Select(i => (LangValueType)new IntLangValue(i, position)),
            default,
            position
        );
    }
}