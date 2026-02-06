using Avalonia.Controls;
using Avalonia.Media;
using FirstUI.Core;

namespace FirstUI.Utils;

/// <summary>
/// 布局辅助工具
/// </summary>
public static class LayoutHelper
{
    /// <summary>
    /// 应用基础样式到 Avalonia 控件
    /// </summary>
    public static void ApplyBaseStyles(Control control, WidgetBase widget)
    {
        if (widget.Width.HasValue)
            control.Width = widget.Width.Value;

        if (widget.Height.HasValue)
            control.Height = widget.Height.Value;

        control.Margin = new Avalonia.Thickness(
            widget.Margin.Left,
            widget.Margin.Top,
            widget.Margin.Right,
            widget.Margin.Bottom
        );

        if (control is Decorator decorator)
        {
            decorator.Padding = new Avalonia.Thickness(
                widget.Padding.Left,
                widget.Padding.Top,
                widget.Padding.Right,
                widget.Padding.Bottom
            );
        }

        control.Opacity = widget.Opacity;
        control.IsVisible = widget.IsVisible;

        if (!string.IsNullOrEmpty(widget.BackgroundColor))
        {
            var brush = ParseColorBrush(widget.BackgroundColor);
            if (control is Panel panel)
                panel.Background = brush;
            else if (control is Border border)
                border.Background = brush;
            else if (control is ContentControl contentControl)
                contentControl.Background = brush;
        }
    }

    /// <summary>
    /// 解析颜色字符串为 Brush
    /// </summary>
    public static IBrush ParseColorBrush(string colorString)
    {
        try
        {
            // 支持格式: #RRGGBB, #AARRGGBB, 颜色名称
            return new SolidColorBrush(Color.Parse(colorString));
        }
        catch
        {
            // 默认返回透明色
            return Brushes.Transparent;
        }
    }

    /// <summary>
    /// 解析颜色字符串为 Color
    /// </summary>
    public static Color ParseColor(string colorString, Color defaultColor = default)
    {
        try
        {
            return Color.Parse(colorString);
        }
        catch
        {
            return defaultColor;
        }
    }
}
