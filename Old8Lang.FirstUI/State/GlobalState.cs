using System.Collections.Concurrent;

namespace Old8Lang.FirstUI.State;

/// <summary>
/// GlobalState 全局状态管理器
/// 提供类似 Flutter Provider / SwiftUI EnvironmentObject 的功能
/// </summary>
public static class GlobalStateManager
{
    private static readonly ConcurrentDictionary<string, IState> GlobalStates = new();
    private static readonly Lock Lock = new();

    /// <summary>
    /// 注册全局状态
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    /// <param name="key">状态键</param>
    /// <param name="initialValue">初始值</param>
    /// <returns>全局状态实例</returns>
    public static GlobalState<T> Register<T>(string key, T? initialValue = default)
    {
        lock (Lock)
        {
            if (GlobalStates.TryGetValue(key, out var existingState))
            {
                if (existingState is GlobalState<T> typedState)
                {
                    return typedState;
                }
                throw new InvalidOperationException($"Global state with key '{key}' already exists with different type");
            }

            var state = new GlobalState<T>(key, initialValue);
            GlobalStates[key] = state;
            return state;
        }
    }

    /// <summary>
    /// 获取全局状态
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    /// <param name="key">状态键</param>
    /// <returns>全局状态实例，如果不存在则返回 null</returns>
    public static GlobalState<T>? Get<T>(string key)
    {
        if (GlobalStates.TryGetValue(key, out var state) && state is GlobalState<T> typedState)
        {
            return typedState;
        }
        return null;
    }

    /// <summary>
    /// 获取或创建全局状态
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    /// <param name="key">状态键</param>
    /// <param name="initialValue">初始值</param>
    /// <returns>全局状态实例</returns>
    public static GlobalState<T> GetOrCreate<T>(string key, T? initialValue = default)
    {
        return Get<T>(key) ?? Register(key, initialValue);
    }

    /// <summary>
    /// 移除全局状态
    /// </summary>
    /// <param name="key">状态键</param>
    /// <returns>是否成功移除</returns>
    public static bool Remove(string key)
    {
        return GlobalStates.TryRemove(key, out _);
    }

    /// <summary>
    /// 清空所有全局状态
    /// </summary>
    public static void Clear()
    {
        GlobalStates.Clear();
    }

    /// <summary>
    /// 检查是否存在指定键的全局状态
    /// </summary>
    public static bool Contains(string key)
    {
        return GlobalStates.ContainsKey(key);
    }

    /// <summary>
    /// 获取所有全局状态的键
    /// </summary>
    public static IEnumerable<string> GetAllKeys()
    {
        return GlobalStates.Keys;
    }

    /// <summary>
    /// 获取全局状态数量
    /// </summary>
    public static int Count => GlobalStates.Count;
}

/// <summary>
/// GlobalState 全局状态类
/// </summary>
/// <typeparam name="T">状态类型</typeparam>
public class GlobalState<T> : ObservableState<T>
{
    /// <summary>
    /// 全局状态的唯一键
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 构造函数（内部使用）
    /// </summary>
    internal GlobalState(string key, T? initialValue = default) : base(initialValue)
    {
        Key = key;
    }

    /// <summary>
    /// 从全局管理器中移除此状态
    /// </summary>
    public void Dispose()
    {
        GlobalStateManager.Remove(Key);
        ClearSubscribers();
    }

    /// <summary>
    /// 克隆为局部状态
    /// </summary>
    public new State<T> Clone()
    {
        return new State<T>(Value);
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString()
    {
        return $"GlobalState[{Key}] = {Value?.ToString() ?? "null"}";
    }
}

/// <summary>
/// GlobalState 扩展方法
/// </summary>
public static class GlobalStateExtensions
{
    /// <summary>
    /// 将局部状态提升为全局状态
    /// </summary>
    public static GlobalState<T> ToGlobal<T>(this State<T> state, string key)
    {
        return GlobalStateManager.Register(key, state.Value);
    }

    /// <summary>
    /// 同步到全局状态
    /// </summary>
    public static void SyncToGlobal<T>(this State<T> state, string key)
    {
        var globalState = GlobalStateManager.GetOrCreate(key, state.Value);

        // 订阅局部状态变化
        state.Subscribe(newValue =>
        {
            globalState.Value = newValue;
        });

        // 订阅全局状态变化
        globalState.Subscribe(newValue =>
        {
            state.Value = newValue;
        });
    }
}
