using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using LayoutHelper = Old8Lang.FirstUI.Utils.LayoutHelper;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Tooltip 工具提示组件
/// </summary>
public class Tooltip : WidgetBase
{
    /// <summary>
    /// 目标组件
    /// </summary>
    public WidgetBase? Child { get; set; }

    /// <summary>
    /// 提示文本
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// 提示内容组件
    /// </summary>
    public WidgetBase? ContentWidget { get; set; }

    /// <summary>
    /// 提示位置
    /// </summary>
    public TooltipPlacement Placement { get; set; } = TooltipPlacement.Top;

    /// <summary>
    /// 显示延迟（毫秒）
    /// </summary>
    public int ShowDelay { get; set; } = 500;

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string TooltipBackground { get; set; } = "#333333";

    /// <summary>
    /// 文字颜色
    /// </summary>
    public string TooltipForeground { get; set; } = "#FFFFFF";

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// 最大宽度
    /// </summary>
    public double MaxWidth { get; set; } = 300;

    public override object Build(BuildContext context)
    {
        if (Child == null)
        {
            return new TextBlock { Text = "[Tooltip requires a child]" };
        }

        // 构建子组件
        var childControl = Child.Build(context);
        if (childControl is not Control control)
        {
            return childControl;
        }

        // 创建提示内容
        Control tooltipContent;
        if (ContentWidget != null)
        {
            var widget = ContentWidget.Build(context);
            tooltipContent = widget as Control ?? new TextBlock { Text = "[Invalid content]" };
        }
        else if (!string.IsNullOrEmpty(Text))
        {
            var border = new Border
            {
                Background = LayoutHelper.ParseColorBrush(TooltipBackground),
                CornerRadius = new CornerRadius(4),
                Padding = new Avalonia.Thickness(8, 6),
                MaxWidth = MaxWidth
            };

            var textBlock = new TextBlock
            {
                Text = Text,
                FontSize = FontSize,
                Foreground = LayoutHelper.ParseColorBrush(TooltipForeground),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            border.Child = textBlock;
            tooltipContent = border;
        }
        else
        {
            tooltipContent = new TextBlock { Text = "[No tooltip content]" };
        }

        // 设置 Avalonia Tooltip
        ToolTip.SetTip(control, tooltipContent);
        ToolTip.SetPlacement(control, GetAvaloniaPlacement());
        ToolTip.SetShowDelay(control, ShowDelay);

        return control;
    }

    private PlacementMode GetAvaloniaPlacement()
    {
        return Placement switch
        {
            TooltipPlacement.Top => PlacementMode.Top,
            TooltipPlacement.Bottom => PlacementMode.Bottom,
            TooltipPlacement.Left => PlacementMode.Left,
            TooltipPlacement.Right => PlacementMode.Right,
            _ => PlacementMode.Top
        };
    }

    /// <summary>
    /// 链式调用：设置子组件
    /// </summary>
    public Tooltip SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    /// <summary>
    /// 链式调用：设置提示文本
    /// </summary>
    public Tooltip SetText(string text)
    {
        Text = text;
        return this;
    }

    /// <summary>
    /// 链式调用：设置提示内容组件
    /// </summary>
    public Tooltip SetContentWidget(WidgetBase widget)
    {
        ContentWidget = widget;
        return this;
    }

    /// <summary>
    /// 链式调用：设置提示位置
    /// </summary>
    public Tooltip SetPlacement(TooltipPlacement placement)
    {
        Placement = placement;
        return this;
    }

    /// <summary>
    /// 链式调用：设置显示延迟
    /// </summary>
    public Tooltip SetShowDelay(int delay)
    {
        ShowDelay = delay;
        return this;
    }

    /// <summary>
    /// 链式调用：设置背景颜色
    /// </summary>
    public Tooltip SetTooltipBackground(string color)
    {
        TooltipBackground = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文字颜色
    /// </summary>
    public Tooltip SetTooltipForeground(string color)
    {
        TooltipForeground = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public Tooltip SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 链式调用：设置最大宽度
    /// </summary>
    public Tooltip SetMaxWidth(double maxWidth)
    {
        MaxWidth = maxWidth;
        return this;
    }
}

/// <summary>
/// Tooltip 位置枚举
/// </summary>
public enum TooltipPlacement
{
    Top,
    Bottom,
    Left,
    Right
}
