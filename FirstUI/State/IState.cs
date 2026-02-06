namespace FirstUI.State;

/// <summary>
/// 状态接口
/// </summary>
public interface IState
{
    /// <summary>
    /// 获取状态值
    /// </summary>
    object? GetValue();

    /// <summary>
    /// 设置状态值
    /// </summary>
    void SetValue(object? value);

    /// <summary>
    /// 状态变化事件
    /// </summary>
    event EventHandler<StateChangedEventArgs>? Changed;
}

/// <summary>
/// 状态变化事件参数
/// </summary>
public class StateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 旧值
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// 新值
    /// </summary>
    public object? NewValue { get; set; }
}
