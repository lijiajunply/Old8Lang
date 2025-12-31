using Avalonia.Controls;
using Avalonia.Layout;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// ListView 列表视图组件，支持虚拟化滚动
/// </summary>
public class ListView : WidgetBase
{
    /// <summary>
    /// 数据源
    /// </summary>
    public List<object>? Items { get; set; }

    /// <summary>
    /// 列表项构建器函数
    /// </summary>
    public Func<object, int, WidgetBase>? ItemBuilder { get; set; }

    /// <summary>
    /// 列表项点击回调
    /// </summary>
    public Action<object, int>? OnItemClick { get; set; }

    /// <summary>
    /// 分隔线高度
    /// </summary>
    public double SeparatorHeight { get; set; } = 1;

    /// <summary>
    /// 分隔线颜色
    /// </summary>
    public string? SeparatorColor { get; set; }

    /// <summary>
    /// 是否显示分隔线
    /// </summary>
    public bool ShowSeparator { get; set; } = true;

    /// <summary>
    /// 列表项间距
    /// </summary>
    public double ItemSpacing { get; set; } = 0;

    public override object Build(BuildContext context)
    {
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(scrollViewer, this);

        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = ItemSpacing
        };

        // 构建列表项
        if (Items != null && ItemBuilder != null)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var index = i;

                // 创建列表项容器
                var itemContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical
                };

                // 使用 ItemBuilder 构建列表项
                var itemWidget = ItemBuilder(item, index);
                var itemControl = itemWidget.Build(context);

                if (itemControl is Control control)
                {
                    // 添加点击事件
                    if (OnItemClick != null)
                    {
                        control.PointerPressed += (sender, e) =>
                        {
                            OnItemClick?.Invoke(item, index);
                        };
                        control.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                    }

                    itemContainer.Children.Add(control);
                }

                // 添加分隔线
                if (ShowSeparator && i < Items.Count - 1)
                {
                    var separator = new Border
                    {
                        Height = SeparatorHeight,
                        Background = string.IsNullOrEmpty(SeparatorColor)
                            ? Utils.LayoutHelper.ParseColorBrush("#E0E0E0")
                            : Utils.LayoutHelper.ParseColorBrush(SeparatorColor)
                    };
                    itemContainer.Children.Add(separator);
                }

                stackPanel.Children.Add(itemContainer);
            }
        }

        scrollViewer.Content = stackPanel;
        return scrollViewer;
    }

    /// <summary>
    /// 链式调用：设置数据源
    /// </summary>
    public ListView SetItems(List<object> items)
    {
        Items = items;
        return this;
    }

    /// <summary>
    /// 链式调用：设置列表项构建器
    /// </summary>
    public ListView SetItemBuilder(Func<object, int, WidgetBase> builder)
    {
        ItemBuilder = builder;
        return this;
    }

    /// <summary>
    /// 链式调用：设置列表项点击回调
    /// </summary>
    public ListView SetOnItemClick(Action<object, int> onClick)
    {
        OnItemClick = onClick;
        return this;
    }

    /// <summary>
    /// 链式调用：设置分隔线样式
    /// </summary>
    public ListView SetSeparator(double height, string? color = null)
    {
        SeparatorHeight = height;
        SeparatorColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示分隔线
    /// </summary>
    public ListView SetShowSeparator(bool show)
    {
        ShowSeparator = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置列表项间距
    /// </summary>
    public ListView SetItemSpacing(double spacing)
    {
        ItemSpacing = spacing;
        return this;
    }
}
