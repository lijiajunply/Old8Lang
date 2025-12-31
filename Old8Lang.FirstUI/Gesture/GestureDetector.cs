using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Old8Lang.FirstUI.Core;
using System;
using System.Diagnostics;

namespace Old8Lang.FirstUI.Gesture;

/// <summary>
/// 手势检测组件 - 检测各种手势并触发相应的回调
/// </summary>
public class GestureDetector : WidgetBase
{
    private Point _dragStartPosition;
    private Point _lastPosition;
    private DateTime _pressStartTime;
    private DateTime _lastTapTime;
    private Vector _totalDelta;
    private bool _isDragging;
    private System.Timers.Timer? _longPressTimer;

    /// <summary>
    /// 子组件
    /// </summary>
    public WidgetBase? Child { get; set; }

    /// <summary>
    /// 单击回调
    /// </summary>
    public Action<GestureEventData>? OnTap { get; set; }

    /// <summary>
    /// 双击回调
    /// </summary>
    public Action<GestureEventData>? OnDoubleTap { get; set; }

    /// <summary>
    /// 长按回调
    /// </summary>
    public Action<GestureEventData>? OnLongPress { get; set; }

    /// <summary>
    /// 拖动开始回调
    /// </summary>
    public Action<GestureEventData>? OnDragStart { get; set; }

    /// <summary>
    /// 拖动中回调
    /// </summary>
    public Action<GestureEventData>? OnDrag { get; set; }

    /// <summary>
    /// 拖动结束回调
    /// </summary>
    public Action<GestureEventData>? OnDragEnd { get; set; }

    /// <summary>
    /// 滑动回调
    /// </summary>
    public Action<GestureEventData>? OnSwipe { get; set; }

    /// <summary>
    /// 长按延迟（毫秒，默认 500ms）
    /// </summary>
    public double LongPressDelay { get; set; } = 500;

    /// <summary>
    /// 拖动阈值（像素，默认 10px）
    /// </summary>
    public double DragThreshold { get; set; } = 10;

    /// <summary>
    /// 滑动速度阈值（像素/秒，默认 500）
    /// </summary>
    public double SwipeVelocityThreshold { get; set; } = 500;

    /// <summary>
    /// 双击时间间隔阈值（毫秒，默认 300ms）
    /// </summary>
    public double DoubleTapDelay { get; set; } = 300;

    public GestureDetector()
    {
        _lastTapTime = DateTime.MinValue;
    }

    public override object Build(BuildContext context)
    {
        var childControl = Child?.Build(context) as Control;
        if (childControl == null)
        {
            childControl = new Border();
        }

        // 创建容器来包装子组件
        var container = new Border
        {
            Child = childControl,
            Background = Avalonia.Media.Brushes.Transparent // 确保可以接收鼠标事件
        };

        // 应用宽度和高度
        if (Width.HasValue) container.Width = Width.Value;
        if (Height.HasValue) container.Height = Height.Value;

        // 注册事件处理器
        container.PointerPressed += OnPointerPressed;
        container.PointerMoved += OnPointerMoved;
        container.PointerReleased += OnPointerReleased;

        return container;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;

        var point = e.GetPosition(control);
        _dragStartPosition = point;
        _lastPosition = point;
        _totalDelta = new Vector(0, 0);
        _pressStartTime = DateTime.Now;
        _isDragging = false;

        // 启动长按计时器
        if (OnLongPress != null)
        {
            _longPressTimer?.Stop();
            _longPressTimer = new System.Timers.Timer(LongPressDelay);
            _longPressTimer.Elapsed += (s, args) =>
            {
                _longPressTimer.Stop();
                if (!_isDragging)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var screenPt = control.PointToScreen(_dragStartPosition);
                        var eventData = new GestureEventData
                        {
                            Type = GestureType.LongPress,
                            Position = _dragStartPosition,
                            ScreenPosition = new Point(screenPt.X, screenPt.Y)
                        };
                        OnLongPress?.Invoke(eventData);
                    });
                }
            };
            _longPressTimer.Start();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control) return;
        if (e.GetCurrentPoint(control).Properties.IsLeftButtonPressed == false) return;

        var currentPosition = e.GetPosition(control);
        var delta = currentPosition - _lastPosition;
        _totalDelta += delta;

        // 检查是否超过拖动阈值
        if (!_isDragging && _totalDelta.Length > DragThreshold)
        {
            _isDragging = true;
            _longPressTimer?.Stop(); // 取消长按

            if (OnDragStart != null)
            {
                var screenPt = control.PointToScreen(currentPosition);
                var eventData = new GestureEventData
                {
                    Type = GestureType.DragStart,
                    Position = currentPosition,
                    ScreenPosition = new Point(screenPt.X, screenPt.Y),
                    Delta = delta,
                    TotalDelta = _totalDelta
                };
                OnDragStart?.Invoke(eventData);
            }
        }

        // 触发拖动事件
        if (_isDragging && OnDrag != null)
        {
            var screenPt = control.PointToScreen(currentPosition);
            var eventData = new GestureEventData
            {
                Type = GestureType.Drag,
                Position = currentPosition,
                ScreenPosition = new Point(screenPt.X, screenPt.Y),
                Delta = delta,
                TotalDelta = _totalDelta
            };
            OnDrag?.Invoke(eventData);
        }

        _lastPosition = currentPosition;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Control control) return;

        _longPressTimer?.Stop();

        var currentPosition = e.GetPosition(control);
        var pressDuration = (DateTime.Now - _pressStartTime).TotalMilliseconds;

        if (_isDragging)
        {
            // 拖动结束
            if (OnDragEnd != null)
            {
                var screenPt = control.PointToScreen(currentPosition);
                var eventData = new GestureEventData
                {
                    Type = GestureType.DragEnd,
                    Position = currentPosition,
                    ScreenPosition = new Point(screenPt.X, screenPt.Y),
                    Delta = currentPosition - _lastPosition,
                    TotalDelta = _totalDelta
                };
                OnDragEnd?.Invoke(eventData);
            }

            // 检测滑动
            if (OnSwipe != null && _totalDelta.Length > DragThreshold)
            {
                var velocity = _totalDelta.Length / pressDuration * 1000; // 像素/秒
                if (velocity > SwipeVelocityThreshold)
                {
                    var swipeDirection = GetSwipeDirection(_totalDelta);
                    var screenPt = control.PointToScreen(currentPosition);
                    var eventData = new GestureEventData
                    {
                        Type = GestureType.Swipe,
                        Position = currentPosition,
                        ScreenPosition = new Point(screenPt.X, screenPt.Y),
                        TotalDelta = _totalDelta,
                        SwipeDirection = swipeDirection,
                        Velocity = velocity
                    };
                    OnSwipe?.Invoke(eventData);
                }
            }
        }
        else
        {
            // 点击事件
            var timeSinceLastTap = (DateTime.Now - _lastTapTime).TotalMilliseconds;

            if (timeSinceLastTap < DoubleTapDelay && OnDoubleTap != null)
            {
                // 双击
                var screenPt = control.PointToScreen(currentPosition);
                var eventData = new GestureEventData
                {
                    Type = GestureType.DoubleTap,
                    Position = currentPosition,
                    ScreenPosition = new Point(screenPt.X, screenPt.Y)
                };
                OnDoubleTap?.Invoke(eventData);
                _lastTapTime = DateTime.MinValue; // 重置，防止三击
            }
            else if (OnTap != null)
            {
                // 单击
                var screenPt = control.PointToScreen(currentPosition);
                var eventData = new GestureEventData
                {
                    Type = GestureType.Tap,
                    Position = currentPosition,
                    ScreenPosition = new Point(screenPt.X, screenPt.Y)
                };
                OnTap?.Invoke(eventData);
                _lastTapTime = DateTime.Now;
            }
        }

        _isDragging = false;
    }

    private SwipeDirection GetSwipeDirection(Vector delta)
    {
        var absX = Math.Abs(delta.X);
        var absY = Math.Abs(delta.Y);

        if (absX > absY)
        {
            return delta.X > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            return delta.Y > 0 ? SwipeDirection.Down : SwipeDirection.Up;
        }
    }

    // 链式调用方法

    public new GestureDetector SetWidth(double width)
    {
        base.SetWidth(width);
        return this;
    }

    public new GestureDetector SetHeight(double height)
    {
        base.SetHeight(height);
        return this;
    }

    public GestureDetector SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    public GestureDetector SetOnTap(Action<GestureEventData> callback)
    {
        OnTap = callback;
        return this;
    }

    public GestureDetector SetOnDoubleTap(Action<GestureEventData> callback)
    {
        OnDoubleTap = callback;
        return this;
    }

    public GestureDetector SetOnLongPress(Action<GestureEventData> callback)
    {
        OnLongPress = callback;
        return this;
    }

    public GestureDetector SetOnDragStart(Action<GestureEventData> callback)
    {
        OnDragStart = callback;
        return this;
    }

    public GestureDetector SetOnDrag(Action<GestureEventData> callback)
    {
        OnDrag = callback;
        return this;
    }

    public GestureDetector SetOnDragEnd(Action<GestureEventData> callback)
    {
        OnDragEnd = callback;
        return this;
    }

    public GestureDetector SetOnSwipe(Action<GestureEventData> callback)
    {
        OnSwipe = callback;
        return this;
    }

    public GestureDetector SetLongPressDelay(double delay)
    {
        LongPressDelay = delay;
        return this;
    }

    public GestureDetector SetDragThreshold(double threshold)
    {
        DragThreshold = threshold;
        return this;
    }

    public GestureDetector SetSwipeVelocityThreshold(double threshold)
    {
        SwipeVelocityThreshold = threshold;
        return this;
    }
}
