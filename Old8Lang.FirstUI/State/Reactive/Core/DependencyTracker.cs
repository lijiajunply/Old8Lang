namespace Old8Lang.FirstUI.State.Reactive.Core;

/// <summary>
/// 依赖追踪器
/// 使用线程静态栈追踪当前正在执行的 Effect
/// </summary>
public static class DependencyTracker
{
    /// <summary>
    /// 当前正在执行的 Effect 栈（线程安全）
    /// </summary>
    [ThreadStatic]
    private static Stack<IEffect>? _effectStack;

    /// <summary>
    /// 是否暂停追踪（线程安全）
    /// </summary>
    [ThreadStatic]
    private static bool _isPaused;

    /// <summary>
    /// 获取 Effect 栈（懒初始化）
    /// </summary>
    private static Stack<IEffect> EffectStack => _effectStack ??= new Stack<IEffect>();

    /// <summary>
    /// 获取当前正在执行的 Effect
    /// </summary>
    public static IEffect? CurrentEffect =>
        !_isPaused && EffectStack.Count > 0 ? EffectStack.Peek() : null;

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    public static bool IsTracking => !_isPaused && EffectStack.Count > 0;

    /// <summary>
    /// 开始追踪
    /// </summary>
    /// <param name="effect">要追踪的 Effect</param>
    public static void StartTracking(IEffect effect)
    {
        EffectStack.Push(effect);
    }

    /// <summary>
    /// 停止追踪
    /// </summary>
    public static void StopTracking()
    {
        if (EffectStack.Count > 0)
        {
            EffectStack.Pop();
        }
    }

    /// <summary>
    /// 在追踪上下文中执行函数
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="effect">要追踪的 Effect</param>
    /// <param name="fn">要执行的函数</param>
    /// <returns>函数返回值</returns>
    public static T? RunWithTracking<T>(IEffect effect, Func<T?> fn)
    {
        StartTracking(effect);
        try
        {
            return fn();
        }
        finally
        {
            StopTracking();
        }
    }

    /// <summary>
    /// 在追踪上下文中执行操作
    /// </summary>
    /// <param name="effect">要追踪的 Effect</param>
    /// <param name="action">要执行的操作</param>
    public static void RunWithTracking(IEffect effect, Action action)
    {
        StartTracking(effect);
        try
        {
            action();
        }
        finally
        {
            StopTracking();
        }
    }

    /// <summary>
    /// 暂停追踪
    /// </summary>
    /// <returns>恢复追踪的 IDisposable</returns>
    public static IDisposable PauseTracking()
    {
        _isPaused = true;
        return new TrackingResumer();
    }

    /// <summary>
    /// 恢复追踪
    /// </summary>
    private static void ResumeTracking()
    {
        _isPaused = false;
    }

    /// <summary>
    /// 追踪恢复器
    /// </summary>
    private sealed class TrackingResumer : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ResumeTracking();
            }
        }
    }
}
