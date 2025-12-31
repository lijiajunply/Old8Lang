using Old8Lang.FirstUI.Theme;

namespace Old8Lang.FirstUI.Core;

/// <summary>
/// 构建上下文
/// 提供组件构建时的环境信息
/// </summary>
public class BuildContext
{
    /// <summary>
    /// 父组件引用
    /// </summary>
    public WidgetBase? Parent { get; set; }

    /// <summary>
    /// 主题配置
    /// </summary>
    public ThemeData? Theme { get; set; }

    /// <summary>
    /// 状态管理器
    /// </summary>
    public StateManager StateManager { get; }

    /// <summary>
    /// 全局状态存储
    /// </summary>
    private readonly Dictionary<string, object> _globalState;

    public BuildContext()
    {
        StateManager = new StateManager();
        _globalState = new Dictionary<string, object>();
    }

    /// <summary>
    /// 获取全局状态
    /// </summary>
    public T? GetGlobalState<T>(string key)
    {
        if (_globalState.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <summary>
    /// 设置全局状态
    /// </summary>
    public void SetGlobalState(string key, object value)
    {
        _globalState[key] = value;
    }

    /// <summary>
    /// 移除全局状态
    /// </summary>
    public void RemoveGlobalState(string key)
    {
        _globalState.Remove(key);
    }
}
