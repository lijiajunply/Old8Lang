using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;

namespace Old8Lang.FirstUI;

/// <summary>
/// 可观察状态基类
/// 提供全局状态管理功能
/// </summary>
public class GlobalState
{
    private static readonly Lazy<GlobalState> _instance = new(() => new GlobalState());
    private readonly Dictionary<string, object> _states = new();
    private readonly Dictionary<string, List<Action<object>>> _listeners = new();

    /// <summary>
    /// 获取全局状态单例实例
    /// </summary>
    public static GlobalState Instance => _instance.Value;

    private GlobalState() { }

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
    public void SetState<T>(string key, T value)
    {
        var oldValue = _states.ContainsKey(key) ? _states[key] : default(T);
        _states[key] = value!;

        // 如果值发生变化，通知所有监听者
        if (!Equals(oldValue, value))
        {
            NotifyListeners(key, value!);
        }
    }

    /// <summary>
    /// 监听状态变化
    /// </summary>
    public void Listen<T>(string key, Action<T> listener)
    {
        if (!_listeners.ContainsKey(key))
        {
            _listeners[key] = new List<Action<object>>();
        }
        _listeners[key].Add(obj => listener((T)obj));
    }

    /// <summary>
    /// 取消监听
    /// </summary>
    public void Unlisten<T>(string key, Action<T> listener)
    {
        if (_listeners.TryGetValue(key, out var callbacks))
        {
            var wrappedCallback = new Action<object>(obj => listener((T)obj));
            callbacks.Remove(wrappedCallback);
        }
    }

    /// <summary>
    /// 通知所有监听者
    /// </summary>
    private void NotifyListeners(string key, object newValue)
    {
        if (_listeners.TryGetValue(key, out var callbacks))
        {
            foreach (var callback in callbacks.ToList()) // ToList 避免迭代时修改
            {
                try
                {
                    callback.Invoke(newValue);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[GlobalState] Error in listener callback: {ex.Message}");
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
/// 计算属性
/// 基于其他状态派生新状态
/// </summary>
public class Computed<T>
{
    private readonly Func<T> _compute;
    private readonly List<WeakReference<Action<T>>> _listeners = new();
    private T? _cachedValue;
    private bool _isDirty = true;

    public Computed(Func<T> compute)
    {
        _compute = compute;
    }

    /// <summary>
    /// 获取计算值
    /// </summary>
    public T Value
    {
        get
        {
            if (_isDirty)
            {
                _cachedValue = _compute();
                _isDirty = false;
            }
            return _cachedValue!;
        }
    }

    /// <summary>
    /// 订阅计算值变化
    /// </summary>
    public void Subscribe(Action<T> listener)
    {
        // 清理无效的弱引用
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            if (!_listeners[i].TryGetTarget(out _))
            {
                _listeners.RemoveAt(i);
            }
        }

        _listeners.Add(new WeakReference<Action<T>>(listener));
    }

    /// <summary>
    /// 标记为需要重新计算
    /// </summary>
    public void Invalidate()
    {
        _isDirty = true;
        NotifyListeners();
    }

    /// <summary>
    /// 通知所有订阅者
    /// </summary>
    private void NotifyListeners()
    {
        var currentValue = Value;
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            if (_listeners[i].TryGetTarget(out var listener))
            {
                try
                {
                    listener(currentValue);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Computed] Error in listener: {ex.Message}");
                }
            }
            else
            {
                _listeners.RemoveAt(i);
            }
        }
    }
}