using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using System;
using Avalonia.Interactivity;

namespace Old8Lang.FirstUI.Gesture;

/// <summary>
/// 拖放目标组件 - 接收拖放操作
/// </summary>
public class DropTarget : WidgetBase
{
    private bool _isHovering;

    /// <summary>
    /// 子组件
    /// </summary>
    public WidgetBase? Child { get; set; }

    /// <summary>
    /// 接受的数据类型（为 null 则接受所有类型）
    /// </summary>
    public string[]? AcceptedDataTypes { get; set; }

    /// <summary>
    /// 拖动进入回调
    /// </summary>
    public Action<DragDropData>? OnDragEnter { get; set; }

    /// <summary>
    /// 拖动悬停回调
    /// </summary>
    public Action<DragDropData>? OnDragOver { get; set; }

    /// <summary>
    /// 拖动离开回调
    /// </summary>
    public Action? OnDragLeave { get; set; }

    /// <summary>
    /// 放置回调
    /// </summary>
    public Action<DragDropData>? OnDrop { get; set; }

    /// <summary>
    /// 是否启用放置（默认 true）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 悬停时的高亮颜色
    /// </summary>
    public string? HoverColor { get; set; }

    /// <summary>
    /// 悬停时的边框颜色
    /// </summary>
    public string? HoverBorderColor { get; set; }

    /// <summary>
    /// 悬停时的边框宽度
    /// </summary>
    public double HoverBorderWidth { get; set; } = 2;

    public override object Build(BuildContext context)
    {
        var childControl = Child?.Build(context) as Control;
        if (childControl == null)
        {
            childControl = new Border
            {
                Width = 200,
                Height = 200,
                Background = Brushes.LightGray
            };
        }

        // 创建容器来包装子组件
        var container = new Border
        {
            Child = childControl,
            Background = Brushes.Transparent
        };

        // 应用宽度和高度
        if (Width.HasValue) container.Width = Width.Value;
        if (Height.HasValue) container.Height = Height.Value;

        // 启用拖放
        DragDrop.SetAllowDrop(container, true);

        // 注册拖放事件处理器
        container.AddHandler(DragDrop.DragEnterEvent, OnDragEnterHandler);
        container.AddHandler(DragDrop.DragOverEvent, OnDragOverHandler);
        container.AddHandler(DragDrop.DragLeaveEvent, OnDragLeaveHandler);
        container.AddHandler(DragDrop.DropEvent, OnDropHandler);

        return container;
    }

    private void OnDragEnterHandler(object? sender, DragEventArgs e)
    {
        if (!Enabled || sender is not Border border) return;

        // 检查数据类型是否被接受
        if (!IsDataTypeAccepted(e))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        _isHovering = true;
        e.DragEffects = DragDropEffects.Copy | DragDropEffects.Move;

        // 应用悬停样式
        ApplyHoverStyle(border, true);

        // 触发回调
        if (OnDragEnter != null)
        {
            var dragData = CreateDragDropData(e);
            OnDragEnter?.Invoke(dragData);
        }

        e.Handled = true;
    }

    private void OnDragOverHandler(object? sender, DragEventArgs e)
    {
        if (!Enabled) return;

        // 检查数据类型是否被接受
        if (!IsDataTypeAccepted(e))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Copy | DragDropEffects.Move;

        // 触发回调
        if (OnDragOver != null)
        {
            var dragData = CreateDragDropData(e);
            OnDragOver?.Invoke(dragData);
        }

        e.Handled = true;
    }

    private void OnDragLeaveHandler(object? sender, RoutedEventArgs e)
    {
        if (!Enabled || sender is not Border border) return;

        _isHovering = false;

        // 移除悬停样式
        ApplyHoverStyle(border, false);

        // 触发回调
        OnDragLeave?.Invoke();

        e.Handled = true;
    }

    private void OnDropHandler(object? sender, DragEventArgs e)
    {
        if (!Enabled || sender is not Border border) return;

        _isHovering = false;

        // 移除悬停样式
        ApplyHoverStyle(border, false);

        // 检查数据类型是否被接受
        if (!IsDataTypeAccepted(e))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        // 触发回调
        if (OnDrop != null)
        {
            var dragData = CreateDragDropData(e);
            OnDrop?.Invoke(dragData);
        }

        e.Handled = true;
    }

    private bool IsDataTypeAccepted(DragEventArgs e)
    {
        // 如果没有指定接受的数据类型，则接受所有
        if (AcceptedDataTypes == null || AcceptedDataTypes.Length == 0)
        {
            return true;
        }

        // 检查是否有匹配的数据格式
        foreach (var dataType in AcceptedDataTypes)
        {
            if (e.Data.Contains(dataType))
            {
                return true;
            }
        }

        return false;
    }

    private DragDropData CreateDragDropData(DragEventArgs e)
    {
        return new DragDropData
        {
            Data = e.Data,
            CurrentPosition = e.GetPosition(null),
            AllowDrop = true
        };
    }

    private void ApplyHoverStyle(Border border, bool isHovering)
    {
        if (!isHovering)
        {
            // 恢复原始样式
            border.BorderBrush = null;
            border.BorderThickness = new Avalonia.Thickness(0);
            if (HoverColor != null)
            {
                border.Background = Brushes.Transparent;
            }
        }
        else
        {
            // 应用悬停样式
            if (HoverBorderColor != null)
            {
                border.BorderBrush = Brush.Parse(HoverBorderColor);
                border.BorderThickness = new Avalonia.Thickness(HoverBorderWidth);
            }
            if (HoverColor != null)
            {
                var hoverBrush = Brush.Parse(HoverColor);
                if (hoverBrush != null)
                {
                    border.Background = hoverBrush;
                }
            }
        }
    }

    // 链式调用方法

    public new DropTarget SetWidth(double width)
    {
        base.SetWidth(width);
        return this;
    }

    public new DropTarget SetHeight(double height)
    {
        base.SetHeight(height);
        return this;
    }

    public DropTarget SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    public DropTarget SetAcceptedDataTypes(params string[] dataTypes)
    {
        AcceptedDataTypes = dataTypes;
        return this;
    }

    public DropTarget SetEnabled(bool enabled)
    {
        Enabled = enabled;
        return this;
    }

    public DropTarget SetHoverColor(string color)
    {
        HoverColor = color;
        return this;
    }

    public DropTarget SetHoverBorderColor(string color)
    {
        HoverBorderColor = color;
        return this;
    }

    public DropTarget SetHoverBorderWidth(double width)
    {
        HoverBorderWidth = width;
        return this;
    }

    public DropTarget SetOnDragEnter(Action<DragDropData> callback)
    {
        OnDragEnter = callback;
        return this;
    }

    public DropTarget SetOnDragOver(Action<DragDropData> callback)
    {
        OnDragOver = callback;
        return this;
    }

    public DropTarget SetOnDragLeave(Action callback)
    {
        OnDragLeave = callback;
        return this;
    }

    public DropTarget SetOnDrop(Action<DragDropData> callback)
    {
        OnDrop = callback;
        return this;
    }
}
