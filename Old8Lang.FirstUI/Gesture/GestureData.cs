using Avalonia;

namespace Old8Lang.FirstUI.Gesture;

/// <summary>
/// 手势类型枚举
/// </summary>
public enum GestureType
{
    /// <summary>单击</summary>
    Tap,
    /// <summary>双击</summary>
    DoubleTap,
    /// <summary>长按</summary>
    LongPress,
    /// <summary>拖动开始</summary>
    DragStart,
    /// <summary>拖动中</summary>
    Drag,
    /// <summary>拖动结束</summary>
    DragEnd,
    /// <summary>滑动</summary>
    Swipe,
    /// <summary>缩放</summary>
    Pinch,
    /// <summary>旋转</summary>
    Rotate
}

/// <summary>
/// 滑动方向枚举
/// </summary>
public enum SwipeDirection
{
    /// <summary>向上</summary>
    Up,
    /// <summary>向下</summary>
    Down,
    /// <summary>向左</summary>
    Left,
    /// <summary>向右</summary>
    Right
}

/// <summary>
/// 手势事件数据
/// </summary>
public class GestureEventData
{
    /// <summary>
    /// 手势类型
    /// </summary>
    public GestureType Type { get; set; }

    /// <summary>
    /// 事件发生的位置（相对于组件）
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// 事件发生的位置（屏幕坐标）
    /// </summary>
    public Point ScreenPosition { get; set; }

    /// <summary>
    /// 拖动的偏移量（仅用于 Drag 事件）
    /// </summary>
    public Vector Delta { get; set; }

    /// <summary>
    /// 拖动的总偏移量（从 DragStart 开始）
    /// </summary>
    public Vector TotalDelta { get; set; }

    /// <summary>
    /// 滑动方向（仅用于 Swipe 事件）
    /// </summary>
    public SwipeDirection? SwipeDirection { get; set; }

    /// <summary>
    /// 滑动速度（像素/秒）
    /// </summary>
    public double Velocity { get; set; }

    /// <summary>
    /// 缩放比例（仅用于 Pinch 事件）
    /// </summary>
    public double Scale { get; set; }

    /// <summary>
    /// 旋转角度（仅用于 Rotate 事件，单位：度）
    /// </summary>
    public double Angle { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否已处理
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// 拖放数据
/// </summary>
public class DragDropData
{
    /// <summary>
    /// 拖动的数据对象
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 数据类型标识符
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// 拖动开始的位置
    /// </summary>
    public Point StartPosition { get; set; }

    /// <summary>
    /// 当前位置
    /// </summary>
    public Point CurrentPosition { get; set; }

    /// <summary>
    /// 是否允许放置
    /// </summary>
    public bool AllowDrop { get; set; } = true;
}
