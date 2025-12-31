namespace Old8Lang.FirstUI.State;

/// <summary>
/// State 局部状态类
/// 用于管理组件的局部状态
/// </summary>
/// <typeparam name="T">状态值类型</typeparam>
public class State<T> : IState
{
    private T? _value;

    /// <summary>
    /// 状态变化事件
    /// </summary>
    public event EventHandler<StateChangedEventArgs>? Changed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="initialValue">初始值</param>
    public State(T? initialValue = default)
    {
        _value = initialValue;
    }

    /// <summary>
    /// 获取状态值
    /// </summary>
    public T? Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                var oldValue = _value;
                _value = value;
                OnChanged(oldValue, _value);
            }
        }
    }

    /// <summary>
    /// 获取状态值（IState 接口实现）
    /// </summary>
    public object? GetValue() => _value;

    /// <summary>
    /// 设置状态值（IState 接口实现）
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
    /// 更新状态值（使用更新函数）
    /// </summary>
    /// <param name="updater">更新函数</param>
    public void Update(Func<T?, T?> updater)
    {
        Value = updater(_value);
    }

    /// <summary>
    /// 触发状态变化事件
    /// </summary>
    protected virtual void OnChanged(T? oldValue, T? newValue)
    {
        Changed?.Invoke(this, new StateChangedEventArgs
        {
            OldValue = oldValue,
            NewValue = newValue
        });
    }

    /// <summary>
    /// 隐式转换为值类型
    /// </summary>
    public static implicit operator T?(State<T> state) => state.Value;

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

        // 返回取消订阅函数
        return () => Changed -= handler;
    }

    /// <summary>
    /// 克隆状态
    /// </summary>
    public State<T> Clone()
    {
        return new State<T>(_value);
    }

    /// <summary>
    /// 重置为初始值
    /// </summary>
    public void Reset()
    {
        Value = default;
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString()
    {
        return _value?.ToString() ?? "null";
    }
}

/// <summary>
/// State 工厂类（提供非泛型创建方法）
/// </summary>
public static class State
{
    /// <summary>
    /// 创建 State 实例
    /// </summary>
    public static State<T> Create<T>(T? initialValue = default)
    {
        return new State<T>(initialValue);
    }

    /// <summary>
    /// 创建 State 实例（Old8Lang 绑定用）
    /// </summary>
    public static IState CreateDynamic(object? initialValue)
    {
        if (initialValue == null)
        {
            return new State<object?>(null);
        }

        var type = initialValue.GetType();
        var stateType = typeof(State<>).MakeGenericType(type);
        return (IState)Activator.CreateInstance(stateType, initialValue)!;
    }
}
