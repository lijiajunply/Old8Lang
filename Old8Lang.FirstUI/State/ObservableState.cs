using Avalonia.Threading;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.State;

/// <summary>
/// ObservableState 可观察状态类
/// 状态变化时自动触发 UI 更新
/// </summary>
/// <typeparam name="T">状态值类型</typeparam>
public class ObservableState<T> : State<T>
{
    private readonly List<WeakReference<WidgetBase>> _subscribers = new();
    private readonly object _lock = new();
    private bool _batchUpdateInProgress = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="initialValue">初始值</param>
    public ObservableState(T? initialValue = default) : base(initialValue)
    {
    }

    /// <summary>
    /// 绑定到组件
    /// </summary>
    /// <param name="widget">要绑定的组件</param>
    public void BindTo(WidgetBase widget)
    {
        lock (_lock)
        {
            // 检查是否已经绑定
            _subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));

            // 添加新订阅者
            _subscribers.Add(new WeakReference<WidgetBase>(widget));
        }
    }

    /// <summary>
    /// 解绑组件
    /// </summary>
    /// <param name="widget">要解绑的组件</param>
    public void UnbindFrom(WidgetBase widget)
    {
        lock (_lock)
        {
            _subscribers.RemoveAll(wr =>
            {
                if (wr.TryGetTarget(out var target))
                {
                    return target == widget;
                }
                return true; // 移除已回收的弱引用
            });
        }
    }

    /// <summary>
    /// 触发状态变化事件
    /// </summary>
    protected override void OnChanged(T? oldValue, T? newValue)
    {
        base.OnChanged(oldValue, newValue);

        // 如果不在批量更新中，通知所有订阅的组件更新
        if (!_batchUpdateInProgress)
        {
            NotifySubscribers();
        }
    }

    /// <summary>
    /// 通知所有订阅者
    /// </summary>
    private void NotifySubscribers()
    {
        lock (_lock)
        {
            // 清理已回收的弱引用
            _subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));

            // 在 UI 线程上触发更新
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    foreach (var weakRef in _subscribers.ToList())
                    {
                        if (weakRef.TryGetTarget(out var widget))
                        {
                            widget.Rebuild();
                        }
                    }
                }
            });
        }
    }

    /// <summary>
    /// 批量更新（避免多次触发 UI 更新）
    /// </summary>
    /// <param name="updates">更新操作</param>
    public void BatchUpdate(Action<ObservableState<T>> updates)
    {
        _batchUpdateInProgress = true;

        try
        {
            updates(this);
        }
        finally
        {
            _batchUpdateInProgress = false;
            NotifySubscribers();
        }
    }

    /// <summary>
    /// 获取订阅者数量
    /// </summary>
    public int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                _subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));
                return _subscribers.Count;
            }
        }
    }

    /// <summary>
    /// 清理所有订阅者
    /// </summary>
    public void ClearSubscribers()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }
}

/// <summary>
/// ObservableState 工厂类
/// </summary>
public static class ObservableState
{
    /// <summary>
    /// 创建 ObservableState 实例
    /// </summary>
    public static ObservableState<T> Create<T>(T? initialValue = default)
    {
        return new ObservableState<T>(initialValue);
    }

    /// <summary>
    /// 创建 ObservableState 实例（Old8Lang 绑定用）
    /// </summary>
    public static IState CreateDynamic(object? initialValue)
    {
        if (initialValue == null)
        {
            return new ObservableState<object?>(null);
        }

        var type = initialValue.GetType();
        var stateType = typeof(ObservableState<>).MakeGenericType(type);
        return (IState)Activator.CreateInstance(stateType, initialValue)!;
    }
}
