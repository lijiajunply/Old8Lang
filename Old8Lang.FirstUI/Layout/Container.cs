using Avalonia.Controls;
using Avalonia.Layout;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Layout;

/// <summary>
/// Container 容器组件
/// 支持内边距、外边距、背景色、边框、圆角等样式
/// </summary>
public class Container(WidgetBase? child = null) : WidgetBase
{
    /// <summary>
    /// 子组件
    /// </summary>
    public WidgetBase? Child { get; set; } = child;

    /// <summary>
    /// 边框圆角
    /// </summary>
    public double BorderRadius { get; set; }

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; }

    /// <summary>
    /// 水平对齐方式
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;

    /// <summary>
    /// 垂直对齐方式
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;

    public override object Build(BuildContext context)
    {
        var border = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(BorderRadius),
            HorizontalAlignment = HorizontalAlignment,
            VerticalAlignment = VerticalAlignment
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(border, this);

        // 设置边框
        if (!string.IsNullOrEmpty(BorderColor) && BorderWidth > 0)
        {
            border.BorderBrush = Utils.LayoutHelper.ParseColorBrush(BorderColor);
            border.BorderThickness = new Avalonia.Thickness(BorderWidth);
        }

        // 设置背景色
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            border.Background = Utils.LayoutHelper.ParseColorBrush(BackgroundColor);
        }

        // 设置内边距
        border.Padding = new Avalonia.Thickness(
            Padding.Left,
            Padding.Top,
            Padding.Right,
            Padding.Bottom
        );

        // 构建子组件
        if (Child != null)
        {
            var childControl = Child.Build(context);
            if (childControl is Control control)
            {
                border.Child = control;
            }
        }

        return border;
    }

    /// <summary>
    /// 链式调用：设置子组件
    /// </summary>
    public Container SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    /// <summary>
    /// 链式调用：设置边框圆角
    /// </summary>
    public Container SetBorderRadius(double radius)
    {
        BorderRadius = radius;
        return this;
    }

    /// <summary>
    /// 链式调用：设置边框
    /// </summary>
    public Container SetBorder(string color, double width)
    {
        BorderColor = color;
        BorderWidth = width;
        return this;
    }

    /// <summary>
    /// 链式调用：设置对齐方式
    /// </summary>
    public Container SetAlignment(HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        HorizontalAlignment = horizontal;
        VerticalAlignment = vertical;
        return this;
    }
}
