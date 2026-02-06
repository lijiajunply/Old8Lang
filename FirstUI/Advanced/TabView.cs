using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FirstUI.Core;

namespace FirstUI.Advanced;

/// <summary>
/// TabView 选项卡视图组件
/// </summary>
public class TabView : WidgetBase
{
    /// <summary>
    /// 选项卡列表
    /// </summary>
    public List<TabItem> Tabs { get; set; } = [];

    /// <summary>
    /// 当前选中的选项卡索引
    /// </summary>
    public int SelectedIndex { get; set; } = 0;

    /// <summary>
    /// 选项卡切换回调
    /// </summary>
    public Action<int>? OnTabChanged { get; set; }

    /// <summary>
    /// 选项卡高度
    /// </summary>
    public double TabHeight { get; set; } = 40;

    /// <summary>
    /// 选项卡字体大小
    /// </summary>
    public double TabFontSize { get; set; } = 14;

    /// <summary>
    /// 选中的选项卡颜色
    /// </summary>
    public string ActiveTabColor { get; set; } = "#2196F3";

    /// <summary>
    /// 未选中的选项卡颜色
    /// </summary>
    public string InactiveTabColor { get; set; } = "#757575";

    /// <summary>
    /// 指示器颜色
    /// </summary>
    public string IndicatorColor { get; set; } = "#2196F3";

    /// <summary>
    /// 指示器高度
    /// </summary>
    public double IndicatorHeight { get; set; } = 3;

    public override object Build(BuildContext context)
    {
        var container = new Grid();

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(container, this);

        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(TabHeight, GridUnitType.Pixel) });
        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 创建选项卡头部
        var tabHeader = CreateTabHeader(context);
        Grid.SetRow(tabHeader, 0);
        container.Children.Add(tabHeader);

        // 创建内容区域
        var contentArea = new Border
        {
            Background = string.IsNullOrEmpty(BackgroundColor)
                ? Utils.LayoutHelper.ParseColorBrush("#FFFFFF")
                : Utils.LayoutHelper.ParseColorBrush(BackgroundColor)
        };

        if (SelectedIndex >= 0 && SelectedIndex < Tabs.Count)
        {
            var selectedTab = Tabs[SelectedIndex];
            if (selectedTab.Content != null)
            {
                var content = selectedTab.Content.Build(context);
                if (content is Control control)
                {
                    contentArea.Child = control;
                }
            }
        }

        Grid.SetRow(contentArea, 1);
        container.Children.Add(contentArea);

        return container;
    }

    private Control CreateTabHeader(BuildContext context)
    {
        var headerPanel = new Grid
        {
            Background = Utils.LayoutHelper.ParseColorBrush("#F5F5F5")
        };

        var tabsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Height = TabHeight
        };

        for (int i = 0; i < Tabs.Count; i++)
        {
            var tab = Tabs[i];
            var index = i;
            var isSelected = i == SelectedIndex;

            var tabButton = new Border
            {
                MinWidth = 100,
                Height = TabHeight,
                Background = Brushes.Transparent,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            var tabContent = new Grid();

            // 选项卡文本
            var tabText = new TextBlock
            {
                Text = tab.Title,
                FontSize = TabFontSize,
                Foreground = isSelected
                    ? Utils.LayoutHelper.ParseColorBrush(ActiveTabColor)
                    : Utils.LayoutHelper.ParseColorBrush(InactiveTabColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = isSelected ? FontWeight.SemiBold : FontWeight.Normal
            };
            tabContent.Children.Add(tabText);

            // 选中指示器
            if (isSelected)
            {
                var indicator = new Border
                {
                    Height = IndicatorHeight,
                    Background = Utils.LayoutHelper.ParseColorBrush(IndicatorColor),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                tabContent.Children.Add(indicator);
            }

            tabButton.Child = tabContent;

            // 添加点击事件
            tabButton.PointerPressed += (sender, e) =>
            {
                SelectedIndex = index;
                OnTabChanged?.Invoke(index);

                // 重新构建以更新UI（实际应用中应该使用状态管理）
                // 这里只是演示基本功能
            };

            tabsPanel.Children.Add(tabButton);
        }

        headerPanel.Children.Add(tabsPanel);
        return headerPanel;
    }

    /// <summary>
    /// 链式调用：添加选项卡
    /// </summary>
    public TabView AddTab(string title, WidgetBase content, string? icon = null)
    {
        Tabs.Add(new TabItem
        {
            Title = title,
            Content = content,
            Icon = icon
        });
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中的选项卡
    /// </summary>
    public TabView SetSelectedIndex(int index)
    {
        SelectedIndex = index;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选项卡切换回调
    /// </summary>
    public TabView SetOnTabChanged(Action<int> callback)
    {
        OnTabChanged = callback;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选项卡高度
    /// </summary>
    public TabView SetTabHeight(double height)
    {
        TabHeight = height;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选项卡字体大小
    /// </summary>
    public TabView SetTabFontSize(double fontSize)
    {
        TabFontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中的选项卡颜色
    /// </summary>
    public TabView SetActiveTabColor(string color)
    {
        ActiveTabColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置未选中的选项卡颜色
    /// </summary>
    public TabView SetInactiveTabColor(string color)
    {
        InactiveTabColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置指示器颜色
    /// </summary>
    public TabView SetIndicatorColor(string color)
    {
        IndicatorColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置指示器高度
    /// </summary>
    public TabView SetIndicatorHeight(double height)
    {
        IndicatorHeight = height;
        return this;
    }
}

/// <summary>
/// 选项卡项
/// </summary>
public class TabItem
{
    /// <summary>
    /// 选项卡标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 选项卡内容
    /// </summary>
    public WidgetBase? Content { get; set; }

    /// <summary>
    /// 选项卡图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
