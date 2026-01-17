using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using LayoutHelper = Old8Lang.FirstUI.Utils.LayoutHelper;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Menu 菜单组件
/// </summary>
public class Menu : WidgetBase
{
    /// <summary>
    /// 菜单项列表
    /// </summary>
    public List<MenuItem> Items { get; set; } = [];

    /// <summary>
    /// 菜单方向
    /// </summary>
    public MenuOrientation Orientation { get; set; } = MenuOrientation.Vertical;

    /// <summary>
    /// 选中的菜单项键
    /// </summary>
    public string? SelectedKey { get; set; }

    /// <summary>
    /// 菜单项点击回调
    /// </summary>
    public Action<MenuItem>? OnItemClick { get; set; }

    /// <summary>
    /// 菜单项高度
    /// </summary>
    public double ItemHeight { get; set; } = 40;

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// 选中颜色
    /// </summary>
    public string ActiveColor { get; set; } = "#2196F3";

    /// <summary>
    /// 悬停颜色
    /// </summary>
    public string HoverColor { get; set; } = "#F5F5F5";

    public override object Build(BuildContext context)
    {
        var container = new StackPanel
        {
            Orientation = Orientation == MenuOrientation.Vertical
                ? Avalonia.Layout.Orientation.Vertical
                : Avalonia.Layout.Orientation.Horizontal
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(container, this);

        // 设置默认背景色
        if (string.IsNullOrEmpty(BackgroundColor))
        {
            var border = new Border
            {
                Background = LayoutHelper.ParseColorBrush("#FFFFFF"),
                Child = container
            };

            // 构建菜单项
            BuildMenuItems(container, Items, context, 0);

            return border;
        }

        // 构建菜单项
        BuildMenuItems(container, Items, context, 0);

        return container;
    }

    private void BuildMenuItems(StackPanel container, List<MenuItem> items, BuildContext context, int level)
    {
        foreach (var item in items)
        {
            var itemBorder = new Border
            {
                Height = ItemHeight,
                Cursor = item.IsEnabled
                    ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    : new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow)
            };

            var isSelected = item.Key == SelectedKey;

            // 设置背景色
            if (isSelected)
            {
                itemBorder.Background = LayoutHelper.ParseColorBrush(ActiveColor);
            }
            else
            {
                itemBorder.Background = Brushes.Transparent;
            }

            var contentPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(16 + level * 20, 0, 16, 0)
            };

            // 图标
            if (!string.IsNullOrEmpty(item.Icon))
            {
                var icon = new TextBlock
                {
                    Text = item.Icon,
                    FontSize = FontSize,
                    Margin = new Avalonia.Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = isSelected
                        ? LayoutHelper.ParseColorBrush("#FFFFFF")
                        : LayoutHelper.ParseColorBrush("#666666")
                };
                contentPanel.Children.Add(icon);
            }

            // 标题
            var titleText = new TextBlock
            {
                Text = item.Title,
                FontSize = FontSize,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = isSelected
                    ? LayoutHelper.ParseColorBrush("#FFFFFF")
                    : LayoutHelper.ParseColorBrush("#333333")
            };
            contentPanel.Children.Add(titleText);

            // 子菜单指示器
            if (item.Children != null && item.Children.Count > 0)
            {
                var indicator = new TextBlock
                {
                    Text = Orientation == MenuOrientation.Vertical ? "›" : "▾",
                    FontSize = 12,
                    Margin = new Avalonia.Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = isSelected
                        ? LayoutHelper.ParseColorBrush("#FFFFFF")
                        : LayoutHelper.ParseColorBrush("#666666")
                };
                contentPanel.Children.Add(indicator);
            }

            itemBorder.Child = contentPanel;

            // 添加悬停效果
            itemBorder.PointerEntered += (s, e) =>
            {
                if (!isSelected && item.IsEnabled)
                {
                    itemBorder.Background = LayoutHelper.ParseColorBrush(HoverColor);
                }
            };

            itemBorder.PointerExited += (s, e) =>
            {
                if (!isSelected)
                {
                    itemBorder.Background = Brushes.Transparent;
                }
            };

            // 添加点击事件
            if (item.IsEnabled)
            {
                itemBorder.PointerPressed += (s, e) =>
                {
                    SelectedKey = item.Key;
                    OnItemClick?.Invoke(item);
                    item.OnClick?.Invoke();
                };
            }

            container.Children.Add(itemBorder);

            // 递归构建子菜单
            if (item.Children != null && item.Children.Count > 0 && item.IsExpanded)
            {
                BuildMenuItems(container, item.Children, context, level + 1);
            }
        }
    }

    /// <summary>
    /// 链式调用：添加菜单项
    /// </summary>
    public Menu AddItem(MenuItem item)
    {
        Items.Add(item);
        return this;
    }

    /// <summary>
    /// 链式调用：设置菜单方向
    /// </summary>
    public Menu SetOrientation(MenuOrientation orientation)
    {
        Orientation = orientation;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中的菜单项
    /// </summary>
    public Menu SetSelectedKey(string key)
    {
        SelectedKey = key;
        return this;
    }

    /// <summary>
    /// 链式调用：设置菜单项点击回调
    /// </summary>
    public Menu SetOnItemClick(Action<MenuItem> callback)
    {
        OnItemClick = callback;
        return this;
    }

    /// <summary>
    /// 链式调用：设置菜单项高度
    /// </summary>
    public Menu SetItemHeight(double height)
    {
        ItemHeight = height;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中颜色
    /// </summary>
    public Menu SetActiveColor(string color)
    {
        ActiveColor = color;
        return this;
    }
}

/// <summary>
/// 菜单项
/// </summary>
public class MenuItem
{
    /// <summary>
    /// 菜单项唯一键
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 子菜单项
    /// </summary>
    public List<MenuItem>? Children { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否展开子菜单
    /// </summary>
    public bool IsExpanded { get; set; } = false;

    /// <summary>
    /// 点击回调
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// 附加数据
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// 菜单方向枚举
/// </summary>
public enum MenuOrientation
{
    Vertical,
    Horizontal
}
