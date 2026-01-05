using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Gesture;

/// <summary>
/// 可拖动组件 - 使子组件可以被拖动
/// </summary>
public class Draggable : WidgetBase
{
    private Point _dragStartPosition;
    private Point _originalPosition;
    private bool _isDragging;
    private Canvas? _canvas;
    private Control? _draggableControl;

    /// <summary>
    /// 子组件
    /// </summary>
    public WidgetBase? Child { get; set; }

    /// <summary>
    /// 拖动的数据（用于拖放操作）
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 数据类型标识符
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// 拖动开始回调
    /// </summary>
    public Action<DragDropData>? OnDragStart { get; set; }

    /// <summary>
    /// 拖动中回调
    /// </summary>
    public Action<DragDropData>? OnDragging { get; set; }

    /// <summary>
    /// 拖动结束回调
    /// </summary>
    public Action<DragDropData>? OnDragEnd { get; set; }

    /// <summary>
    /// 是否启用拖动（默认 true）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 拖动轴限制（None = 不限制，Horizontal = 仅水平，Vertical = 仅垂直）
    /// </summary>
    public DragAxis Axis { get; set; } = DragAxis.None;

    /// <summary>
    /// 拖动反馈类型（Default = 默认，Shadow = 阴影，Opacity = 半透明）
    /// </summary>
    public DragFeedback Feedback { get; set; } = DragFeedback.Default;

    /// <summary>
    /// 拖动时的不透明度（仅当 Feedback = Opacity 时有效）
    /// </summary>
    public double DragOpacity { get; set; } = 0.5;

    public override object Build(BuildContext context)
    {
        var childControl = Child?.Build(context) as Control;
        if (childControl == null)
        {
            childControl = new Border
            {
                Width = 100,
                Height = 100,
                Background = Avalonia.Media.Brushes.LightGray
            };
        }

        _draggableControl = childControl;

        // 使用 Canvas 来允许自由定位
        _canvas = new Canvas
        {
            ClipToBounds = false
        };

        // 应用宽度和高度
        if (Width.HasValue) _canvas.Width = Width.Value;
        if (Height.HasValue) _canvas.Height = Height.Value;

        // 将子组件添加到 Canvas
        _canvas.Children.Add(childControl);

        // 设置初始位置
        Canvas.SetLeft(childControl, 0);
        Canvas.SetTop(childControl, 0);
        _originalPosition = new Point(0, 0);

        // 注册事件处理器
        childControl.PointerPressed += OnPointerPressed;
        childControl.PointerMoved += OnPointerMoved;
        childControl.PointerReleased += OnPointerReleased;
        childControl.PointerCaptureLost += OnPointerCaptureLost;

        return _canvas;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!Enabled || sender is not Control control) return;

        _isDragging = true;
        _dragStartPosition = e.GetPosition(_canvas);
        _originalPosition = new Point(Canvas.GetLeft(control), Canvas.GetTop(control));

        // 捕获指针
        control.PointerPressed += (s, args) => e.Pointer.Capture(control);

        // 应用拖动反馈
        ApplyDragFeedback(control, true);

        // 触发拖动开始回调
        if (OnDragStart != null)
        {
            var dragData = new DragDropData
            {
                Data = Data,
                DataType = DataType,
                StartPosition = _dragStartPosition,
                CurrentPosition = _dragStartPosition
            };
            OnDragStart?.Invoke(dragData);
        }

        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || !Enabled || sender is not Control control) return;

        var currentPosition = e.GetPosition(_canvas);
        var delta = currentPosition - _dragStartPosition;

        // 应用轴限制
        double newX = _originalPosition.X;
        double newY = _originalPosition.Y;

        switch (Axis)
        {
            case DragAxis.None:
                newX += delta.X;
                newY += delta.Y;
                break;
            case DragAxis.Horizontal:
                newX += delta.X;
                break;
            case DragAxis.Vertical:
                newY += delta.Y;
                break;
        }

        // 更新位置
        Canvas.SetLeft(control, newX);
        Canvas.SetTop(control, newY);

        // 触发拖动中回调
        if (OnDragging != null)
        {
            var dragData = new DragDropData
            {
                Data = Data,
                DataType = DataType,
                StartPosition = _dragStartPosition,
                CurrentPosition = currentPosition
            };
            OnDragging?.Invoke(dragData);
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging || sender is not Control control) return;

        _isDragging = false;

        // 释放指针捕获
        e.Pointer.Capture(null);

        // 恢复拖动反馈
        ApplyDragFeedback(control, false);

        // 触发拖动结束回调
        if (OnDragEnd != null)
        {
            var currentPosition = e.GetPosition(_canvas);
            var dragData = new DragDropData
            {
                Data = Data,
                DataType = DataType,
                StartPosition = _dragStartPosition,
                CurrentPosition = currentPosition
            };
            OnDragEnd?.Invoke(dragData);
        }

        e.Handled = true;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_isDragging || sender is not Control control) return;

        _isDragging = false;
        ApplyDragFeedback(control, false);
    }

    private void ApplyDragFeedback(Control control, bool isDragging)
    {
        switch (Feedback)
        {
            case DragFeedback.Opacity:
                control.Opacity = isDragging ? DragOpacity : 1.0;
                break;
            case DragFeedback.Shadow:
                // 应用阴影效果
                if (isDragging)
                {
                    // 拖动时添加阴影
                    var shadow = new BoxShadows(new BoxShadow
                    {
                        Blur = 16,
                        Spread = 0,
                        OffsetX = 0,
                        OffsetY = 4,
                        Color = Color.FromArgb(100, 0, 0, 0)
                    });

                    if (control is Border border)
                    {
                        border.BoxShadow = shadow;
                    }
                    else
                    {
                        // 如果不是 Border，尝试包装一个 Border
                        // 注意：这种情况下效果可能不理想，建议用户直接使用 Border 作为子组件
                        control.RenderTransform = new TranslateTransform(0, 2);
                    }
                }
                else
                {
                    // 恢复时移除阴影
                    if (control is Border border)
                    {
                        border.BoxShadow = new BoxShadows();
                    }
                    else
                    {
                        control.RenderTransform = null;
                    }
                }
                break;
            case DragFeedback.Default:
            default:
                // 不应用特殊效果
                break;
        }
    }

    // 链式调用方法

    public new Draggable SetWidth(double width)
    {
        base.SetWidth(width);
        return this;
    }

    public new Draggable SetHeight(double height)
    {
        base.SetHeight(height);
        return this;
    }

    public Draggable SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    public Draggable SetData(object data)
    {
        Data = data;
        return this;
    }

    public Draggable SetDataType(string dataType)
    {
        DataType = dataType;
        return this;
    }

    public Draggable SetEnabled(bool enabled)
    {
        Enabled = enabled;
        return this;
    }

    public Draggable SetAxis(DragAxis axis)
    {
        Axis = axis;
        return this;
    }

    public Draggable SetFeedback(DragFeedback feedback)
    {
        Feedback = feedback;
        return this;
    }

    public Draggable SetDragOpacity(double opacity)
    {
        DragOpacity = opacity;
        return this;
    }

    public Draggable SetOnDragStart(Action<DragDropData> callback)
    {
        OnDragStart = callback;
        return this;
    }

    public Draggable SetOnDragging(Action<DragDropData> callback)
    {
        OnDragging = callback;
        return this;
    }

    public Draggable SetOnDragEnd(Action<DragDropData> callback)
    {
        OnDragEnd = callback;
        return this;
    }
}

/// <summary>
/// 拖动轴限制
/// </summary>
public enum DragAxis
{
    /// <summary>不限制（可任意方向拖动）</summary>
    None,
    /// <summary>仅水平拖动</summary>
    Horizontal,
    /// <summary>仅垂直拖动</summary>
    Vertical
}

/// <summary>
/// 拖动反馈类型
/// </summary>
public enum DragFeedback
{
    /// <summary>默认（无特殊效果）</summary>
    Default,
    /// <summary>半透明效果</summary>
    Opacity,
    /// <summary>阴影效果</summary>
    Shadow
}
