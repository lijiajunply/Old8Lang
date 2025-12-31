namespace Old8Lang.FirstUI.Animation;

/// <summary>
/// 动画接口
/// </summary>
public interface IAnimation
{
    /// <summary>
    /// 动画时长（毫秒）
    /// </summary>
    int Duration { get; }

    /// <summary>
    /// 动画状态
    /// </summary>
    AnimationStatus Status { get; }

    /// <summary>
    /// 启动动画
    /// </summary>
    void Start();

    /// <summary>
    /// 暂停动画
    /// </summary>
    void Pause();

    /// <summary>
    /// 恢复动画
    /// </summary>
    void Resume();

    /// <summary>
    /// 停止动画
    /// </summary>
    void Stop();

    /// <summary>
    /// 重置动画
    /// </summary>
    void Reset();
}

/// <summary>
/// 动画状态枚举
/// </summary>
public enum AnimationStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    Idle,

    /// <summary>
    /// 运行中
    /// </summary>
    Running,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped
}

/// <summary>
/// 动画完成事件参数
/// </summary>
public class AnimationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// 是否被取消
    /// </summary>
    public bool IsCancelled { get; set; }
}
