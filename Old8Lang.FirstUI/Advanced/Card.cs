using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Card 卡片组件
/// </summary>
public class Card(WidgetBase? child = null) : WidgetBase
{
    /// <summary>
    /// 子组件
    /// </summary>
    public WidgetBase? Child { get; set; } = child;

    /// <summary>
    /// 卡片标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 卡片副标题
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// 圆角半径
    /// </summary>
    public double BorderRadius { get; set; } = 8;

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; } = 1;

    /// <summary>
    /// 阴影高度（模拟海拔效果）
    /// </summary>
    public double Elevation { get; set; } = 2;

    /// <summary>
    /// 是否可点击
    /// </summary>
    public bool IsClickable { get; set; }

    /// <summary>
    /// 点击回调
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// 标题字体大小
    /// </summary>
    public double TitleFontSize { get; set; } = 18;

    /// <summary>
    /// 副标题字体大小
    /// </summary>
    public double SubtitleFontSize { get; set; } = 14;

    public override object Build(BuildContext context)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(BorderRadius),
            Background = string.IsNullOrEmpty(BackgroundColor)
                ? LayoutHelper.ParseColorBrush("#FFFFFF")
                : LayoutHelper.ParseColorBrush(BackgroundColor)
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(border, this);

        // 设置边框
        if (!string.IsNullOrEmpty(BorderColor))
        {
            border.BorderBrush = LayoutHelper.ParseColorBrush(BorderColor);
            border.BorderThickness = new Avalonia.Thickness(BorderWidth);
        }
        else
        {
            border.BorderBrush = LayoutHelper.ParseColorBrush("#E0E0E0");
            border.BorderThickness = new Avalonia.Thickness(1);
        }

        // 设置阴影效果（模拟海拔）
        if (Elevation > 0)
        {
            border.BoxShadow = new BoxShadows(
                new BoxShadow
                {
                    Blur = Elevation * 2,
                    Spread = 0,
                    OffsetX = 0,
                    OffsetY = Elevation,
                    Color = Color.FromArgb(40, 0, 0, 0)
                });
        }

        // 设置默认内边距
        if (Padding.Left == 0 && Padding.Top == 0 && Padding.Right == 0 && Padding.Bottom == 0)
        {
            border.Padding = new Avalonia.Thickness(16);
        }

        // 创建内容容器
        var contentPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Spacing = 8
        };

        // 添加标题
        if (!string.IsNullOrEmpty(Title))
        {
            var titleBlock = new TextBlock
            {
                Text = Title,
                FontSize = TitleFontSize,
                FontWeight = Avalonia.Media.FontWeight.SemiBold
            };
            contentPanel.Children.Add(titleBlock);
        }

        // 添加副标题
        if (!string.IsNullOrEmpty(Subtitle))
        {
            var subtitleBlock = new TextBlock
            {
                Text = Subtitle,
                FontSize = SubtitleFontSize,
                Foreground = LayoutHelper.ParseColorBrush("#757575")
            };
            contentPanel.Children.Add(subtitleBlock);
        }

        // 添加子组件
        if (Child != null)
        {
            var childControl = Child.Build(context);
            if (childControl is Control control)
            {
                contentPanel.Children.Add(control);
            }
        }

        border.Child = contentPanel;

        // 添加点击事件
        if (IsClickable || OnClick != null)
        {
            border.PointerPressed += (sender, e) =>
            {
                OnClick?.Invoke();
            };
            border.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        }

        return border;
    }

    /// <summary>
    /// 链式调用：设置子组件
    /// </summary>
    public Card SetChild(WidgetBase child)
    {
        Child = child;
        return this;
    }

    /// <summary>
    /// 链式调用：设置标题
    /// </summary>
    public Card SetTitle(string title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    /// 链式调用：设置副标题
    /// </summary>
    public Card SetSubtitle(string subtitle)
    {
        Subtitle = subtitle;
        return this;
    }

    /// <summary>
    /// 链式调用：设置圆角
    /// </summary>
    public Card SetBorderRadius(double radius)
    {
        BorderRadius = radius;
        return this;
    }

    /// <summary>
    /// 链式调用：设置边框
    /// </summary>
    public Card SetBorder(string color, double width)
    {
        BorderColor = color;
        BorderWidth = width;
        return this;
    }

    /// <summary>
    /// 链式调用：设置海拔高度
    /// </summary>
    public Card SetElevation(double elevation)
    {
        Elevation = elevation;
        return this;
    }

    /// <summary>
    /// 链式调用：设置点击回调
    /// </summary>
    public Card SetOnClick(Action onClick)
    {
        OnClick = onClick;
        IsClickable = true;
        return this;
    }

    /// <summary>
    /// 链式调用：设置标题字体大小
    /// </summary>
    public Card SetTitleFontSize(double fontSize)
    {
        TitleFontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 链式调用：设置副标题字体大小
    /// </summary>
    public Card SetSubtitleFontSize(double fontSize)
    {
        SubtitleFontSize = fontSize;
        return this;
    }
}
