namespace Old8Lang.FirstUI.Utils;

/// <summary>
/// 回调函数桥接器
/// 管理 Old8Lang 函数和 C# 委托之间的桥接，处理生命周期
/// </summary>
public class CallbackBridge
{
    private static readonly Lazy<CallbackBridge> _instance = new(() => new CallbackBridge());

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static CallbackBridge Instance => _instance.Value;

    /// <summary>
    /// 回调函数引用表
    /// </summary>
    private readonly Dictionary<string, WeakReference> _callbacks = new();

    /// <summary>
    /// 回调函数计数器（用于生成唯一ID）
    /// </summary>
    private int _callbackIdCounter = 0;

    private CallbackBridge() { }

    /// <summary>
    /// 注册回调函数并返回唯一标识符
    /// </summary>
    public string RegisterCallback(object callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        var id = $"callback_{Interlocked.Increment(ref _callbackIdCounter)}_{Guid.NewGuid():N}";
        _callbacks[id] = new WeakReference(callback);

        return id;
    }

    /// <summary>
    /// 获取已注册的回调函数
    /// </summary>
    public object? GetCallback(string id)
    {
        if (string.IsNullOrEmpty(id) || !_callbacks.TryGetValue(id, out var weakRef))
            return null;

        if (weakRef.IsAlive)
        {
            return weakRef.Target;
        }
        else
        {
            // 清理已失效的回调
            _callbacks.Remove(id);
            return null;
        }
    }

    /// <summary>
    /// 取消注册回调函数
    /// </summary>
    public void UnregisterCallback(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            _callbacks.Remove(id);
        }
    }

    /// <summary>
    /// 清理所有失效的回调引用
    /// </summary>
    public void CleanupDeadReferences()
    {
        var deadKeys = _callbacks
            .Where(kvp => !kvp.Value.IsAlive)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in deadKeys)
        {
            _callbacks.Remove(key);
        }
    }

    /// <summary>
    /// 清除所有回调
    /// </summary>
    public void Clear()
    {
        _callbacks.Clear();
    }

    /// <summary>
    /// 获取当前注册的回调数量
    /// </summary>
    public int Count => _callbacks.Count;
}

/// <summary>
/// 回调函数包装器
/// 提供类型安全的回调函数包装
/// </summary>
/// <typeparam name="T">回调参数类型</typeparam>
public class CallbackWrapper<T>
{
    private readonly string _callbackId;
    private readonly Action<T> _wrappedCallback;

    public CallbackWrapper(object old8Callback)
    {
        _callbackId = CallbackBridge.Instance.RegisterCallback(old8Callback);
        _wrappedCallback = TypeConverter.WrapAction<T>(old8Callback);
    }

    /// <summary>
    /// 调用回调函数
    /// </summary>
    public void Invoke(T arg)
    {
        var callback = CallbackBridge.Instance.GetCallback(_callbackId);
        if (callback != null)
        {
            _wrappedCallback.Invoke(arg);
        }
    }

    /// <summary>
    /// 释放回调引用
    /// </summary>
    public void Dispose()
    {
        CallbackBridge.Instance.UnregisterCallback(_callbackId);
    }
}

/// <summary>
/// 无参回调函数包装器
/// </summary>
public class CallbackWrapper
{
    private readonly string _callbackId;
    private readonly Action _wrappedCallback;

    public CallbackWrapper(object old8Callback)
    {
        _callbackId = CallbackBridge.Instance.RegisterCallback(old8Callback);
        _wrappedCallback = TypeConverter.WrapAction(old8Callback);
    }

    /// <summary>
    /// 调用回调函数
    /// </summary>
    public void Invoke()
    {
        var callback = CallbackBridge.Instance.GetCallback(_callbackId);
        if (callback != null)
        {
            _wrappedCallback.Invoke();
        }
    }

    /// <summary>
    /// 释放回调引用
    /// </summary>
    public void Dispose()
    {
        CallbackBridge.Instance.UnregisterCallback(_callbackId);
    }
}
