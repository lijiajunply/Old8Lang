using Old8Lang.FirstUI.State.Reactive.Core;

namespace Old8Lang.FirstUI.State.Reactive;

/// <summary>
/// AutoComputed 自动计算属性
/// Vue 风格的计算属性，自动追踪依赖并在依赖变化时重新计算
/// </summary>
/// <typeparam name="T">计算结果类型</typeparam>
public class AutoComputed<T> : IReactiveSource, IEffect, IState
{
    private readonly Func<T?> _getter;
    private T? _value;
    private bool _dirty = true;
    private bool _isComputing;
    private bool _isDisposed;

    private readonly HashSet<IEffect> _subscribers = [];
    private readonly HashSet<IReactiveSource> _dependencies = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// 状态变化事件（IState 接口）
    /// </summary>
    public event EventHandler<StateChangedEventArgs>? Changed;

    /// <summary>
    /// 是否已释放
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="getter">计算函数</param>
    public AutoComputed(Func<T?> getter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }

    /// <summary>
    /// 获取计算值
    /// 懒计算：只在访问时计算，使用 _dirty 标记
    /// </summary>
    public T? Value
    {
        get
        {
            Track();
            if (_dirty)
            {
                Compute();
            }
            return _value;
        }
    }

    /// <summary>
    /// 计算值
    /// </summary>
    private void Compute()
    {
        if (_isComputing)
        {
            throw new InvalidOperationException("Circular dependency detected in AutoComputed");
        }

        try
        {
            _isComputing = true;

            // 清除旧依赖
            ClearDependencies();

            // 在追踪上下文中执行计算函数
            var newValue = DependencyTracker.RunWithTracking(this, _getter);

            // 检查值是否变化
            if (!EqualityComparer<T>.Default.Equals(_value, newValue))
            {
                var oldValue = _value;
                _value = newValue;
                _dirty = false;
                OnChanged(oldValue, newValue);
            }
            else
            {
                _dirty = false;
            }
        }
        finally
        {
            _isComputing = false;
        }
    }

    /// <summary>
    /// 追踪当前访问
    /// </summary>
    public void Track()
    {
        var currentEffect = DependencyTracker.CurrentEffect;
        if (currentEffect != null && currentEffect != this)
        {
            AddSubscriber(currentEffect);
            currentEffect.AddDependency(this);
        }
    }

    /// <summary>
    /// 触发更新（标记为脏并通知订阅者）
    /// </summary>
    public void Trigger()
    {
        if (_dirty) return; // 已经是脏的，无需重复触发

        _dirty = true;

        List<IEffect> effectsToRun;
        lock (_lock)
        {
            effectsToRun = _subscribers.Where(e => !e.IsDisposed).ToList();
        }

        foreach (var effect in effectsToRun)
        {
            effect.Run();
        }
    }

    /// <summary>
    /// 添加订阅者
    /// </summary>
    public void AddSubscriber(IEffect effect)
    {
        lock (_lock)
        {
            _subscribers.Add(effect);
        }
    }

    /// <summary>
    /// 移除订阅者
    /// </summary>
    public void RemoveSubscriber(IEffect effect)
    {
        lock (_lock)
        {
            _subscribers.Remove(effect);
        }
    }

    /// <summary>
    /// 执行副作用（重新计算）
    /// </summary>
    public void Run()
    {
        if (_isDisposed) return;

        // 标记为脏，下次访问时重新计算
        _dirty = true;

        // 如果有订阅者，触发它们
        Trigger();
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
    /// 触发状态变化事件
    /// </summary>
    private void OnChanged(T? oldValue, T? newValue)
    {
        Changed?.Invoke(this, new StateChangedEventArgs
        {
            OldValue = oldValue,
            NewValue = newValue
        });
    }

    /// <summary>
    /// 获取状态值（IState 接口）
    /// </summary>
    public object? GetValue() => Value;

    /// <summary>
    /// 设置状态值（IState 接口）- 计算属性不支持直接设置
    /// </summary>
    public void SetValue(object? value)
    {
        throw new InvalidOperationException("Cannot directly set the value of an AutoComputed property");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        ClearDependencies();

        lock (_lock)
        {
            _subscribers.Clear();
        }
    }

    /// <summary>
    /// 隐式转换为值类型
    /// </summary>
    public static implicit operator T?(AutoComputed<T> computed) => computed.Value;

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString() => $"AutoComputed = {Value?.ToString() ?? "null"}";
}

/// <summary>
/// AutoComputed 工厂类
/// </summary>
public static class AutoComputed
{
    /// <summary>
    /// 创建自动计算属性
    /// </summary>
    public static AutoComputed<T> Create<T>(Func<T?> getter)
    {
        return new AutoComputed<T>(getter);
    }
}
