using FirstUI.State.Reactive.Core;

namespace FirstUI.State.Reactive;

/// <summary>
/// Watch 选项
/// </summary>
public class WatchOptions
{
    /// <summary>
    /// 是否立即执行回调（默认 false）
    /// </summary>
    public bool Immediate { get; set; }

    /// <summary>
    /// 是否深度监听（默认 false）
    /// </summary>
    public bool Deep { get; set; }
}

/// <summary>
/// Watch 监听器
/// 监听特定响应式源的变化并执行回调
/// </summary>
/// <typeparam name="T">监听值类型</typeparam>
public class Watch<T> : IEffect
{
    private readonly Func<T?> _source;
    private readonly Action<T?, T?> _callback;
    private readonly WatchOptions _options;
    private T? _oldValue;
    private bool _isDisposed;
    private readonly HashSet<IReactiveSource> _dependencies = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// 是否已释放
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="source">数据源函数</param>
    /// <param name="callback">回调函数 (newValue, oldValue)</param>
    /// <param name="options">选项</param>
    public Watch(Func<T?> source, Action<T?, T?> callback, WatchOptions? options = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _options = options ?? new WatchOptions();

        // 收集依赖并获取初始值
        _oldValue = DependencyTracker.RunWithTracking(this, _source);

        // 如果设置了 immediate，立即执行回调
        if (_options.Immediate)
        {
            ExecuteCallback(_oldValue, default);
        }
    }

    /// <summary>
    /// 执行副作用
    /// </summary>
    public void Run()
    {
        if (_isDisposed) return;

        // 清除旧依赖
        ClearDependencies();

        // 重新收集依赖并获取新值
        var newValue = DependencyTracker.RunWithTracking(this, _source);

        // 检查值是否变化
        if (!EqualityComparer<T>.Default.Equals(_oldValue, newValue) || _options.Deep)
        {
            var oldValue = _oldValue;
            _oldValue = newValue;
            ExecuteCallback(newValue, oldValue);
        }
    }

    /// <summary>
    /// 执行回调
    /// </summary>
    private void ExecuteCallback(T? newValue, T? oldValue)
    {
        _callback(newValue, oldValue);
    }

    /// <summary>
    /// 添加依赖
    /// </summary>
    public void AddDependency(IReactiveSource source)
    {
        lock (_lock)
        {
            _dependencies.Add(source);
        }
    }

    /// <summary>
    /// 清除所有依赖
    /// </summary>
    public void ClearDependencies()
    {
        lock (_lock)
        {
            foreach (var dep in _dependencies)
            {
                dep.RemoveSubscriber(this);
            }
            _dependencies.Clear();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        ClearDependencies();
    }
}

/// <summary>
/// Watch 工厂类
/// </summary>
public static class Watch
{
    /// <summary>
    /// 创建 Watch 实例
    /// </summary>
    /// <typeparam name="T">监听值类型</typeparam>
    /// <param name="source">数据源函数</param>
    /// <param name="callback">回调函数</param>
    /// <param name="options">选项</param>
    /// <returns>Watch 实例（可用于停止监听）</returns>
    public static Watch<T> Create<T>(Func<T?> source, Action<T?, T?> callback, WatchOptions? options = null)
    {
        return new Watch<T>(source, callback, options);
    }

    /// <summary>
    /// 监听 Ref 的变化
    /// </summary>
    public static Watch<T> Create<T>(Ref<T> source, Action<T?, T?> callback, WatchOptions? options = null)
    {
        return new Watch<T>(() => source.Value, callback, options);
    }

    /// <summary>
    /// 监听 AutoComputed 的变化
    /// </summary>
    public static Watch<T> Create<T>(AutoComputed<T> source, Action<T?, T?> callback, WatchOptions? options = null)
    {
        return new Watch<T>(() => source.Value, callback, options);
    }

    /// <summary>
    /// 监听多个源的变化
    /// </summary>
    public static Watch<(T1?, T2?)> Create<T1, T2>(
        Func<T1?> source1,
        Func<T2?> source2,
        Action<(T1?, T2?), (T1?, T2?)> callback,
        WatchOptions? options = null)
    {
        return new Watch<(T1?, T2?)>(
            () => (source1(), source2()),
            callback,
            options);
    }

    /// <summary>
    /// 监听多个 Ref 的变化
    /// </summary>
    public static Watch<(T1?, T2?)> Create<T1, T2>(
        Ref<T1> source1,
        Ref<T2> source2,
        Action<(T1?, T2?), (T1?, T2?)> callback,
        WatchOptions? options = null)
    {
        return new Watch<(T1?, T2?)>(
            () => (source1.Value, source2.Value),
            callback,
            options);
    }

    /// <summary>
    /// 监听三个源的变化
    /// </summary>
    public static Watch<(T1?, T2?, T3?)> Create<T1, T2, T3>(
        Func<T1?> source1,
        Func<T2?> source2,
        Func<T3?> source3,
        Action<(T1?, T2?, T3?), (T1?, T2?, T3?)> callback,
        WatchOptions? options = null)
    {
        return new Watch<(T1?, T2?, T3?)>(
            () => (source1(), source2(), source3()),
            callback,
            options);
    }
}
