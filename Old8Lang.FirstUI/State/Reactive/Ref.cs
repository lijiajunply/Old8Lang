using Old8Lang.FirstUI.State.Reactive.Core;

namespace Old8Lang.FirstUI.State.Reactive;

/// <summary>
/// Ref 响应式引用
/// Vue 风格的响应式引用，支持自动依赖追踪
/// </summary>
/// <typeparam name="T">值类型</typeparam>
public class Ref<T> : IReactiveSource, IState
{
    private T? _value;
    private readonly HashSet<IEffect> _subscribers = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// 状态变化事件（IState 接口）
    /// </summary>
    public event EventHandler<StateChangedEventArgs>? Changed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="initialValue">初始值</param>
    public Ref(T? initialValue = default)
    {
        _value = initialValue;
    }

    /// <summary>
    /// 获取或设置值
    /// 读取时自动追踪依赖，写入时自动触发更新
    /// </summary>
    public T? Value
    {
        get
        {
            Track();
            return _value;
        }
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            var oldValue = _value;
            _value = value;
            Trigger(); // 先触发依赖更新，让 AutoComputed 标记为脏
            OnChanged(oldValue, value); // 再触发 UI 重建
        }
    }

    /// <summary>
    /// 追踪当前访问
    /// </summary>
    public void Track()
    {
        var currentEffect = DependencyTracker.CurrentEffect;
        if (currentEffect != null)
        {
            AddSubscriber(currentEffect);
            currentEffect.AddDependency(this);
        }
    }

    /// <summary>
    /// 触发更新
    /// </summary>
    public void Trigger()
    {
        List<IEffect> effectsToRun;
        lock (_lock)
        {
            // 复制订阅者列表，避免在迭代时修改
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
    /// 触发状态变化事件
    /// </summary>
    private void OnChanged(T? oldValue, T? newValue)
    {
        Changed?.Invoke(this, new StateChangedEventArgs
        {
            OldValue = oldValue,
            NewValue = newValue
        });

        // 触发 UI 重建
        FirstUIBinding.RebuildUI();
    }

    /// <summary>
    /// 获取状态值（IState 接口）
    /// </summary>
    public object? GetValue() => Value;

    /// <summary>
    /// 设置状态值（IState 接口）
    /// </summary>
    public void SetValue(object? value)
    {
        if (value is T typedValue)
        {
            Value = typedValue;
        }
        else if (value == null)
        {
            Value = default;
        }
        else
        {
            throw new InvalidCastException($"Cannot convert {value.GetType()} to {typeof(T)}");
        }
    }

    /// <summary>
    /// 更新值（使用更新函数）
    /// </summary>
    /// <param name="updater">更新函数</param>
    public void Update(Func<T?, T?> updater)
    {
        Value = updater(_value);
    }

    /// <summary>
    /// 隐式转换为值类型
    /// </summary>
    public static implicit operator T?(Ref<T> r) => r.Value;

    /// <summary>
    /// 订阅状态变化
    /// </summary>
    /// <param name="listener">监听器函数</param>
    /// <returns>取消订阅的 Action</returns>
    public Action Subscribe(Action<T?> listener)
    {
        EventHandler<StateChangedEventArgs> handler = (sender, e) =>
        {
            if (e.NewValue is T newValue)
            {
                listener(newValue);
            }
            else if (e.NewValue == null)
            {
                listener(default);
            }
        };

        Changed += handler;
        return () => Changed -= handler;
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString() => _value?.ToString() ?? "null";
}

/// <summary>
/// Ref 工厂类
/// </summary>
public static class Ref
{
    /// <summary>
    /// 创建 Ref 实例
    /// </summary>
    public static Ref<T> Create<T>(T? initialValue = default)
    {
        return new Ref<T>(initialValue);
    }

    /// <summary>
    /// 从现有 State 创建 Ref
    /// </summary>
    public static Ref<T> FromState<T>(State<T> state)
    {
        var r = new Ref<T>(state.Value);

        // 双向同步
        state.Subscribe(newValue => r.Value = newValue);
        r.Subscribe(newValue =>
        {
            if (state.Value is null && newValue is null) return;
            if (state.Value?.Equals(newValue) == true) return;
            state.Value = newValue;
        });

        return r;
    }

    /// <summary>
    /// 创建动态类型的 Ref（Old8Lang 绑定用）
    /// </summary>
    public static IState CreateDynamic(object? initialValue)
    {
        if (initialValue == null)
        {
            return new Ref<object?>(null);
        }

        var type = initialValue.GetType();
        var refType = typeof(Ref<>).MakeGenericType(type);
        return (IState)Activator.CreateInstance(refType, initialValue)!;
    }
}