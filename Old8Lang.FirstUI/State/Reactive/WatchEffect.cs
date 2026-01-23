using Old8Lang.FirstUI.State.Reactive.Core;

namespace Old8Lang.FirstUI.State.Reactive;

/// <summary>
/// WatchEffect 副作用监听器
/// 自动追踪依赖并在依赖变化时重新执行
/// </summary>
public class WatchEffect : IEffect
{
    private readonly Action _effect;
    private Action? _cleanup;
    private bool _isDisposed;
    private bool _isRunning;
    private readonly HashSet<IReactiveSource> _dependencies = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// 是否已释放
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="effect">副作用函数</param>
    public WatchEffect(Action effect)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));

        // 立即执行一次以收集依赖
        Run();
    }

    /// <summary>
    /// 构造函数（带清理函数注册）
    /// </summary>
    /// <param name="effect">副作用函数，接收清理函数注册器</param>
    public WatchEffect(Action<Action<Action>> effect)
    {
        // 包装 effect 以支持清理函数注册
        _effect = () =>
        {
            effect(cleanup => _cleanup = cleanup);
        };

        // 立即执行一次以收集依赖
        Run();
    }

    /// <summary>
    /// 执行副作用
    /// </summary>
    public void Run()
    {
        if (_isDisposed || _isRunning) return;

        try
        {
            _isRunning = true;

            // 执行清理函数
            _cleanup?.Invoke();
            _cleanup = null;

            // 清除旧依赖
            ClearDependencies();

            // 执行副作用函数
            DependencyTracker.RunWithTracking(this, _effect);
        }
        finally
        {
            _isRunning = false;
        }
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

        // 执行清理函数
        _cleanup?.Invoke();
        _cleanup = null;

        ClearDependencies();
    }
}

/// <summary>
/// WatchEffect 工厂类
/// </summary>
public static class WatchEffectFactory
{
    /// <summary>
    /// 创建 WatchEffect 实例
    /// </summary>
    /// <param name="effect">副作用函数</param>
    /// <returns>WatchEffect 实例（可用于停止监听）</returns>
    public static WatchEffect Create(Action effect)
    {
        return new WatchEffect(effect);
    }

    /// <summary>
    /// 创建 WatchEffect 实例（带清理函数注册）
    /// </summary>
    /// <param name="effect">副作用函数，接收清理函数注册器</param>
    /// <returns>WatchEffect 实例（可用于停止监听）</returns>
    public static WatchEffect Create(Action<Action<Action>> effect)
    {
        return new WatchEffect(effect);
    }
}
