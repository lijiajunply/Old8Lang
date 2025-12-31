namespace Old8Lang.FirstUI;

/// <summary>
/// 状态管理器
/// 管理组件状态并触发 UI 更新
/// </summary>
public class StateManager
{
    private readonly Dictionary<string, object> _states = new();
    private readonly Dictionary<string, List<Action>> _listeners = new();

    /// <summary>
    /// 获取状态值
    /// </summary>
    public T? GetState<T>(string key)
    {
        if (_states.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// 设置状态值并通知监听者
    /// </summary>
    public void SetState(string key, object value)
    {
        var oldValue = _states.ContainsKey(key) ? _states[key] : null;
        _states[key] = value;

        // 如果值发生变化，通知所有监听者
        if (!Equals(oldValue, value))
        {
            NotifyListeners(key);
        }
    }

    /// <summary>
    /// 监听状态变化
    /// </summary>
    public void Listen(string key, Action callback)
    {
        if (!_listeners.ContainsKey(key))
        {
            _listeners[key] = new List<Action>();
        }
        _listeners[key].Add(callback);
    }

    /// <summary>
    /// 取消监听
    /// </summary>
    public void Unlisten(string key, Action callback)
    {
        if (_listeners.TryGetValue(key, out var callbacks))
        {
            callbacks.Remove(callback);
        }
    }

    /// <summary>
    /// 通知所有监听者
    /// </summary>
    private void NotifyListeners(string key)
    {
        if (_listeners.TryGetValue(key, out var callbacks))
        {
            foreach (var callback in callbacks.ToList()) // ToList 避免迭代时修改
            {
                try
                {
                    callback.Invoke();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[StateManager] Error in listener callback: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 清除所有状态
    /// </summary>
    public void Clear()
    {
        _states.Clear();
        _listeners.Clear();
    }
}

/// <summary>
/// 可观察状态
/// 封装状态值并提供响应式更新
/// </summary>
/// <typeparam name="T">状态值类型</typeparam>
public class ObservableState<T>
{
    private T _value;
    private readonly List<Action<T>> _listeners = new();

    public T Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                NotifyListeners();
            }
        }
    }

    public ObservableState(T initialValue)
    {
        _value = initialValue;
    }

    /// <summary>
    /// 订阅状态变化
    /// </summary>
    public void Subscribe(Action<T> listener)
    {
        _listeners.Add(listener);
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe(Action<T> listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>
    /// 通知所有订阅者
    /// </summary>
    private void NotifyListeners()
    {
        foreach (var listener in _listeners.ToList())
        {
            try
            {
                listener.Invoke(_value);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ObservableState] Error in listener: {ex.Message}");
            }
        }
    }

    public override string ToString()
    {
        return _value?.ToString() ?? "null";
    }
}
