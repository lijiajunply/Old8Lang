using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using LayoutHelper = Old8Lang.FirstUI.Utils.LayoutHelper;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Breadcrumb 面包屑导航组件
/// </summary>
public class Breadcrumb : WidgetBase
{
    /// <summary>
    /// 面包屑项列表
    /// </summary>
    public List<BreadcrumbItem> Items { get; set; } = new();

    /// <summary>
    /// 分隔符
    /// </summary>
    public string Separator { get; set; } = "/";

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// 默认文本颜色
    /// </summary>
    public string TextColor { get; set; } = "#666666";

    /// <summary>
    /// 激活文本颜色
    /// </summary>
    public string ActiveColor { get; set; } = "#2196F3";

    /// <summary>
    /// 悬停颜色
    /// </summary>
    public string HoverColor { get; set; } = "#1976D2";

    /// <summary>
    /// 项目点击回调
    /// </summary>
    public Action<BreadcrumbItem, int>? OnItemClick { get; set; }

    public override object Build(BuildContext context)
    {
        var container = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(container, this);

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var index = i;
            var isLast = i == Items.Count - 1;

            // 创建项目容器
            var itemPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8
            };

            // 创建项目文本
            var textBlock = new TextBlock
            {
                Text = item.Title,
                FontSize = FontSize,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = isLast
                    ? LayoutHelper.ParseColorBrush(ActiveColor)
                    : LayoutHelper.ParseColorBrush(TextColor)
            };

            // 如果不是最后一项且可点击，添加点击效果
            if (!isLast || item.IsClickable)
            {
                var button = new Avalonia.Controls.Button
                {
                    Content = textBlock,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),
                    Padding = new Avalonia.Thickness(0),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                };

                // 悬停效果
                button.PointerEntered += (s, e) =>
                {
                    textBlock.Foreground = LayoutHelper.ParseColorBrush(HoverColor);
                    textBlock.TextDecorations = Avalonia.Media.TextDecorations.Underline;
                };

                button.PointerExited += (s, e) =>
                {
                    textBlock.Foreground = isLast
                        ? LayoutHelper.ParseColorBrush(ActiveColor)
                        : LayoutHelper.ParseColorBrush(TextColor);
                    textBlock.TextDecorations = null;
                };

                // 点击事件
                button.Click += (s, e) =>
                {
                    OnItemClick?.Invoke(item, index);
                    item.OnClick?.Invoke();
                };

                itemPanel.Children.Add(button);
            }
            else
            {
                itemPanel.Children.Add(textBlock);
            }

            // 添加分隔符（不是最后一项）
            if (!isLast)
            {
                var separator = new TextBlock
                {
                    Text = Separator,
                    FontSize = FontSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = LayoutHelper.ParseColorBrush("#CCCCCC")
                };
                itemPanel.Children.Add(separator);
            }

            container.Children.Add(itemPanel);
        }

        return container;
    }

    /// <summary>
    /// 链式调用：添加面包屑项
    /// </summary>
    public Breadcrumb AddItem(BreadcrumbItem item)
    {
        Items.Add(item);
        return this;
    }

    /// <summary>
    /// 链式调用：添加面包屑项（简化版）
    /// </summary>
    public Breadcrumb AddItem(string title, Action? onClick = null)
    {
        Items.Add(new BreadcrumbItem
        {
            Title = title,
            OnClick = onClick
        });
        return this;
    }

    /// <summary>
    /// 链式调用：设置分隔符
    /// </summary>
    public Breadcrumb SetSeparator(string separator)
    {
        Separator = separator;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public Breadcrumb SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本颜色
    /// </summary>
    public Breadcrumb SetTextColor(string color)
    {
        TextColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置激活颜色
    /// </summary>
    public Breadcrumb SetActiveColor(string color)
    {
        ActiveColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置项目点击回调
    /// </summary>
    public Breadcrumb SetOnItemClick(Action<BreadcrumbItem, int> callback)
    {
        OnItemClick = callback;
        return this;
    }
}

/// <summary>
/// 面包屑项
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 路径或键
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 是否可点击
    /// </summary>
    public bool IsClickable { get; set; } = true;

    /// <summary>
    /// 点击回调
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// 附加数据
    /// </summary>
    public object? Data { get; set; }
}
