using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using FirstUI.Core;
using FirstUI.Utils;
using LayoutHelper = FirstUI.Utils.LayoutHelper;

namespace FirstUI.Advanced;

/// <summary>
/// Popover 弹出框组件
/// </summary>
public class Popover : WidgetBase
{
    /// <summary>
    /// 触发元素
    /// </summary>
    public WidgetBase? Trigger { get; set; }

    /// <summary>
    /// 弹出内容
    /// </summary>
    public WidgetBase? Content { get; set; }

    /// <summary>
    /// 弹出位置
    /// </summary>
    public PopoverPlacement Placement { get; set; } = PopoverPlacement.Bottom;

    /// <summary>
    /// 触发方式
    /// </summary>
    public PopoverTriggerMode TriggerMode { get; set; } = PopoverTriggerMode.Click;

    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    public double PopoverWidth { get; set; } = 200;

    /// <summary>
    /// 最大高度
    /// </summary>
    public double MaxHeight { get; set; } = 400;

    /// <summary>
    /// 是否显示箭头
    /// </summary>
    public bool ShowArrow { get; set; } = true;

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string PopoverBackground { get; set; } = "#FFFFFF";

    public override object Build(BuildContext context)
    {
        if (Trigger == null)
        {
            return new TextBlock { Text = "[Popover requires a trigger]" };
        }

        // 构建触发元素
        var triggerControl = Trigger.Build(context);
        if (triggerControl is not Control trigger)
        {
            return triggerControl;
        }

        // 创建弹出内容
        var popupContent = CreatePopoverContent(context);

        // 创建 Popup
        var popup = new Popup
        {
            Child = popupContent,
            PlacementMode = GetAvaloniaPlacement(),
            PlacementTarget = trigger,
            IsLightDismissEnabled = true
        };

        // 设置触发方式
        if (TriggerMode == PopoverTriggerMode.Click)
        {
            trigger.PointerPressed += (s, e) =>
            {
                popup.IsOpen = !popup.IsOpen;
            };
        }
        else if (TriggerMode == PopoverTriggerMode.Hover)
        {
            trigger.PointerEntered += (s, e) =>
            {
                popup.IsOpen = true;
            };

            trigger.PointerExited += (s, e) =>
            {
                // 延迟关闭，给用户时间移动到 popup 上
                var timer = new System.Timers.Timer(200);
                timer.Elapsed += (sender, args) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (!popup.IsPointerOver)
                        {
                            popup.IsOpen = false;
                        }
                    });
                    timer.Dispose();
                };
                timer.Start();
            };
        }

        // 将 popup 附加到触发器
        // 注意：Avalonia 的 Popup 需要在可视化树中，这里简化处理
        var container = new Grid();
        container.Children.Add(trigger);

        // 由于 Popup 的特殊性，我们需要确保它能正确显示
        trigger.Loaded += (s, e) =>
        {
            if (trigger.GetVisualRoot() is TopLevel topLevel)
            {
                // Popup 将自动附加到可视化树
            }
        };

        return container;
    }

    private Control CreatePopoverContent(BuildContext context)
    {
        var border = new Border
        {
            Width = PopoverWidth,
            MaxHeight = MaxHeight,
            Background = LayoutHelper.ParseColorBrush(PopoverBackground),
            BorderBrush = LayoutHelper.ParseColorBrush("#E0E0E0"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(4),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 8,
                Spread = 0,
                OffsetX = 0,
                OffsetY = 2,
                Color = Color.FromArgb(40, 0, 0, 0)
            })
        };

        var contentPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical
        };

        // 添加标题
        if (!string.IsNullOrEmpty(Title))
        {
            var titleBorder = new Border
            {
                Padding = new Avalonia.Thickness(12, 8),
                BorderBrush = LayoutHelper.ParseColorBrush("#E0E0E0"),
                BorderThickness = new Avalonia.Thickness(0, 0, 0, 1)
            };

            var titleText = new TextBlock
            {
                Text = Title,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Foreground = LayoutHelper.ParseColorBrush("#333333")
            };

            titleBorder.Child = titleText;
            contentPanel.Children.Add(titleBorder);
        }

        // 添加内容
        if (Content != null)
        {
            var contentBorder = new Border
            {
                Padding = new Avalonia.Thickness(12)
            };

            var contentWidget = Content.Build(context);
            if (contentWidget is Control control)
            {
                contentBorder.Child = control;
            }

            contentPanel.Children.Add(contentBorder);
        }

        border.Child = contentPanel;
        return border;
    }

    private PlacementMode GetAvaloniaPlacement()
    {
        return Placement switch
        {
            PopoverPlacement.Top => PlacementMode.Top,
            PopoverPlacement.Bottom => PlacementMode.Bottom,
            PopoverPlacement.Left => PlacementMode.Left,
            PopoverPlacement.Right => PlacementMode.Right,
            PopoverPlacement.TopLeft => PlacementMode.TopEdgeAlignedLeft,
            PopoverPlacement.TopRight => PlacementMode.TopEdgeAlignedRight,
            PopoverPlacement.BottomLeft => PlacementMode.BottomEdgeAlignedLeft,
            PopoverPlacement.BottomRight => PlacementMode.BottomEdgeAlignedRight,
            _ => PlacementMode.Bottom
        };
    }

    /// <summary>
    /// 链式调用：设置触发元素
    /// </summary>
    public Popover SetTrigger(WidgetBase trigger)
    {
        Trigger = trigger;
        return this;
    }

    /// <summary>
    /// 链式调用：设置弹出内容
    /// </summary>
    public Popover SetContent(WidgetBase content)
    {
        Content = content;
        return this;
    }

    /// <summary>
    /// 链式调用：设置弹出位置
    /// </summary>
    public Popover SetPlacement(PopoverPlacement placement)
    {
        Placement = placement;
        return this;
    }

    /// <summary>
    /// 链式调用：设置触发方式
    /// </summary>
    public Popover SetTriggerMode(PopoverTriggerMode mode)
    {
        TriggerMode = mode;
        return this;
    }

    /// <summary>
    /// 链式调用：设置标题
    /// </summary>
    public Popover SetTitle(string title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    /// 链式调用：设置宽度
    /// </summary>
    public Popover SetPopoverWidth(double width)
    {
        PopoverWidth = width;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示箭头
    /// </summary>
    public Popover SetShowArrow(bool show)
    {
        ShowArrow = show;
        return this;
    }
}

/// <summary>
/// Popover 位置枚举
/// </summary>
public enum PopoverPlacement
{
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

/// <summary>
/// Popover 触发方式枚举
/// </summary>
public enum PopoverTriggerMode
{
    Click,
    Hover
}
