using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Old8Lang.Error;

namespace Old8Lang.Compiler.Helpers;

/// <summary>
/// IL 模式异步生成器的产出端：由生成器的异步状态机在 <c>yield</c> 处写入元素。
/// </summary>
public sealed class IlAsyncYieldEmitter
{
    private readonly Channel<object?> _channel = Channel.CreateUnbounded<object?>(
        new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

    private readonly CancellationToken _cancellationToken;
    private readonly Lock _sync = new();
    private TaskCompletionSource? _resumeTcs;
    private bool _cancelled;

    internal IlAsyncYieldEmitter(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    internal ChannelReader<object?> Reader => _channel.Reader;

    internal bool IsCancellationRequested => _cancellationToken.IsCancellationRequested || _cancelled;

    /// <summary>
    /// 以非阻塞方式产出一个元素到异步流，并返回等待消费者推进下一次迭代的 <see cref="Task"/>。
    /// 若消费者已取消迭代则返回已取消任务，以便恢复后抛出 <see cref="OperationCanceledException"/> 触发生成器内的 <c>finally</c>。
    /// </summary>
    public Task YieldAsync(object? value)
    {
        TaskCompletionSource resumeTcs;
        lock (_sync)
        {
            if (_cancelled || _cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(_cancellationToken.IsCancellationRequested
                    ? _cancellationToken
                    : new CancellationToken(canceled: true));
            }

            resumeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _resumeTcs = resumeTcs;
        }

        _channel.Writer.TryWrite(value);
        return resumeTcs.Task;
    }

    /// <summary>
    /// 同步兼容入口：委托给 <see cref="YieldAsync"/>。状态机生成的 IL 统一调用非阻塞的 <see cref="YieldAsync"/>。
    /// </summary>
    public void Yield(object? value) => YieldAsync(value).GetAwaiter().GetResult();

    internal void Resume()
    {
        TaskCompletionSource? tcs;
        lock (_sync)
        {
            tcs = _resumeTcs;
            _resumeTcs = null;
        }

        tcs?.TrySetResult();
    }

    internal void Cancel()
    {
        TaskCompletionSource? tcs;
        lock (_sync)
        {
            _cancelled = true;
            tcs = _resumeTcs;
            _resumeTcs = null;
        }

        tcs?.TrySetCanceled(_cancellationToken.IsCancellationRequested
            ? _cancellationToken
            : new CancellationToken(canceled: true));
    }

    internal void Complete() => _channel.Writer.TryComplete();

    internal void Fail(Exception exception) => _channel.Writer.TryComplete(exception);
}

/// <summary>
/// IL 模式异步生成器返回的 <see cref="IAsyncEnumerable{T}"/> 包装：
/// 延迟启动底层异步状态机，按序流出 <c>yield</c> 的元素并在对应位置重抛异常。
/// </summary>
public sealed class IlAsyncGeneratorStream(Func<IlAsyncYieldEmitter, Task<object?>> producerFactory)
    : IAsyncEnumerable<object?>
{
    public IAsyncEnumerator<object?> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new Enumerator(producerFactory, cancellationToken);

    private sealed class Enumerator : IAsyncEnumerator<object?>
    {
        private readonly Func<IlAsyncYieldEmitter, Task<object?>> _producerFactory;
        private readonly CancellationTokenSource _cts;
        private readonly IlAsyncYieldEmitter _emitter;
        private readonly CancellationTokenRegistration _cancelRegistration;
        private Task? _producerTask;
        private object? _current;
        private bool _disposed;
        private bool _hasCurrent;

        public Enumerator(Func<IlAsyncYieldEmitter, Task<object?>> producerFactory, CancellationToken cancellationToken)
        {
            _producerFactory = producerFactory;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _emitter = new IlAsyncYieldEmitter(_cts.Token);
            _cancelRegistration = _cts.Token.Register(
                static state => ((IlAsyncYieldEmitter)state!).Cancel(),
                _emitter);
        }

        public object? Current => _current;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_disposed) return false;

            if (_producerTask is null)
                _producerTask = RunProducerAsync();
            else if (_hasCurrent)
                _emitter.Resume();
            _hasCurrent = false;

            if (_emitter.Reader.TryRead(out var immediate))
            {
                _current = immediate;
                _hasCurrent = true;
                return true;
            }

            while (await _emitter.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                if (_emitter.Reader.TryRead(out var item))
                {
                    _current = item;
                    _hasCurrent = true;
                    return true;
                }
            }

            return false;
        }

        private async Task RunProducerAsync()
        {
            try
            {
                await _producerFactory(_emitter).ConfigureAwait(false);
                _emitter.Complete();
            }
            catch (OperationCanceledException) when (_emitter.IsCancellationRequested)
            {
                _emitter.Complete();
            }
            catch (Exception ex)
            {
                _emitter.Fail(ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            if (_producerTask is not null) await _producerTask.ConfigureAwait(false);
            await _cancelRegistration.DisposeAsync().ConfigureAwait(false);
            _cts.Dispose();
        }
    }
}

/// <summary>
/// IL 模式 <c>async for-in</c> 的统一运行期枚举器适配器，
/// 同时兼容 <see cref="IAsyncEnumerable{T}"/> 与普通 <see cref="IEnumerable"/>。
/// </summary>
public sealed class IlAsyncLoopEnumerator : IDisposable
{
    private readonly IAsyncEnumerator<object?>? _asyncEnumerator;
    private readonly Func<Task<bool>>? _genericMoveNext;
    private readonly Func<object?>? _genericCurrent;
    private readonly Func<ValueTask>? _genericDispose;
    private readonly IEnumerator? _syncEnumerator;

    private IlAsyncLoopEnumerator(IAsyncEnumerator<object?> asyncEnumerator)
    {
        _asyncEnumerator = asyncEnumerator;
    }

    private IlAsyncLoopEnumerator(
        Func<Task<bool>> genericMoveNext,
        Func<object?> genericCurrent,
        Func<ValueTask> genericDispose)
    {
        _genericMoveNext = genericMoveNext;
        _genericCurrent = genericCurrent;
        _genericDispose = genericDispose;
    }

    private IlAsyncLoopEnumerator(IEnumerator syncEnumerator)
    {
        _syncEnumerator = syncEnumerator;
    }

    /// <summary>
    /// 按运行时对象形态创建统一的异步循环枚举器。
    /// </summary>
    public static IlAsyncLoopEnumerator Create(object? source)
    {
        if (source is null)
        {
            throw new InvalidOperationError(new SourcePosition(), "async for-in 的目标对象为 null，无法迭代");
        }

        if (source is IAsyncEnumerable<object?> asyncObjEnumerable)
        {
            return new IlAsyncLoopEnumerator(asyncObjEnumerable.GetAsyncEnumerator());
        }

        var asyncEnumInterface = source.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

        if (asyncEnumInterface is not null)
        {
            var elementType = asyncEnumInterface.GetGenericArguments()[0];
            var getEnumeratorMethod = asyncEnumInterface.GetMethod("GetAsyncEnumerator", [typeof(CancellationToken)])!;
            var enumerator = getEnumeratorMethod.Invoke(source, [CancellationToken.None])!;
            var enumeratorInterface = typeof(IAsyncEnumerator<>).MakeGenericType(elementType);
            var moveNextMethod = enumeratorInterface.GetMethod("MoveNextAsync")!;
            var currentProperty = enumeratorInterface.GetProperty("Current")!;
            var disposeMethod = typeof(IAsyncDisposable).GetMethod("DisposeAsync")!;

            return new IlAsyncLoopEnumerator(
                () => ((ValueTask<bool>)moveNextMethod.Invoke(enumerator, null)!).AsTask(),
                () => currentProperty.GetValue(enumerator),
                () => (ValueTask)disposeMethod.Invoke(enumerator, null)!);
        }

        if (source is IEnumerable syncEnumerable)
        {
            return new IlAsyncLoopEnumerator(syncEnumerable.GetEnumerator());
        }

        throw new InvalidOperationError(new SourcePosition(),
            $"async for-in 的目标对象不可枚举，实际类型: {source.GetType().Name}");
    }

    public object? Current
    {
        get
        {
            if (_asyncEnumerator is not null) return _asyncEnumerator.Current;
            if (_genericCurrent is not null) return _genericCurrent();
            return _syncEnumerator?.Current;
        }
    }

    public Task<bool> MoveNextTask()
    {
        if (_asyncEnumerator is not null) return _asyncEnumerator.MoveNextAsync().AsTask();
        if (_genericMoveNext is not null) return _genericMoveNext();
        return Task.FromResult(_syncEnumerator?.MoveNext() ?? false);
    }

    public bool MoveNextBlocking()
    {
        return MoveNextTask().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_asyncEnumerator is not null)
        {
            _asyncEnumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return;
        }

        if (_genericDispose is not null)
        {
            _genericDispose().AsTask().GetAwaiter().GetResult();
            return;
        }

        (_syncEnumerator as IDisposable)?.Dispose();
    }

    /// <summary>
    /// 单变量迭代时若元素为字典项则取 Key，否则原样返回。
    /// </summary>
    public static object? ExtractSingleOrSelf(object? current)
    {
        if (current is DictionaryEntry entry) return entry.Key;
        if (current is not null)
        {
            var type = current.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return type.GetProperty("Key")?.GetValue(current);
            }
        }

        return current;
    }

    /// <summary>
    /// 从当前元素中按位置提取多变量解构字段（支持 DictionaryEntry / KeyValuePair / ITuple / IList）。
    /// </summary>
    public static object? ExtractItem(object? current, int index)
    {
        if (current is null) return null;

        if (current is DictionaryEntry entry)
        {
            return index == 0 ? entry.Key : index == 1 ? entry.Value : null;
        }

        var type = current.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            return index == 0
                ? type.GetProperty("Key")?.GetValue(current)
                : index == 1
                    ? type.GetProperty("Value")?.GetValue(current)
                    : null;
        }

        if (current is ITuple tuple)
        {
            return index >= 0 && index < tuple.Length ? tuple[index] : null;
        }

        if (current is IList list)
        {
            return index >= 0 && index < list.Count ? list[index] : null;
        }

        return index == 0 ? current : null;
    }
}
