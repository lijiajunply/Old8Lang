using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Input;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// NavigationView 导航视图组件
/// 提供侧边栏导航功能，支持多级菜单
/// </summary>
public class NavigationView : WidgetBase
{
    /// <summary>
    /// 导航菜单项列表
    /// </summary>
    public List<NavigationItem> MenuItems { get; set; } = [];

    /// <summary>
    /// 当前选中项的值
    /// </summary>
    public string? SelectedValue { get; set; }

    /// <summary>
    /// 选择变化回调
    /// </summary>
    public Action<string?>? OnSelectionChanged { get; set; }

    /// <summary>
    /// 内容区域
    /// </summary>
    public WidgetBase? Content { get; set; }

    /// <summary>
    /// 侧边栏宽度
    /// </summary>
    public double SidebarWidth { get; set; } = 250;

    /// <summary>
    /// 侧边栏背景色
    /// </summary>
    public string? SidebarBackgroundColor { get; set; }

    /// <summary>
    /// 选中项背景色
    /// </summary>
    public string? SelectedItemBackgroundColor { get; set; }

    /// <summary>
    /// 悬停项背景色
    /// </summary>
    public string? HoverItemBackgroundColor { get; set; }

    /// <summary>
    /// 文本颜色
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// 是否显示顶部标题栏
    /// </summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// 顶部标题
    /// </summary>
    public string? HeaderTitle { get; set; }

    /// <summary>
    /// 是否可折叠侧边栏
    /// </summary>
    public bool Collapsible { get; set; } = false;

    /// <summary>
    /// 侧边栏位置（左侧或右侧）
    /// </summary>
    public NavigationSidebarPosition SidebarPosition { get; set; } = NavigationSidebarPosition.Left;

    /// <summary>
    /// 显示模式（展开/折叠）
    /// </summary>
    public NavigationDisplayMode DisplayMode { get; set; } = NavigationDisplayMode.Expanded;

    public override object Build(BuildContext context)
    {
        var mainGrid = new Grid();
        
        // 定义列：侧边栏 + 内容区域
        var sidebarColumn = new ColumnDefinition
        {
            Width = new GridLength(DisplayMode == NavigationDisplayMode.Compact ? 60 : SidebarWidth)
        };
        var contentColumn = new ColumnDefinition { Width = GridLength.Star };
        
        if (SidebarPosition == NavigationSidebarPosition.Left)
        {
            mainGrid.ColumnDefinitions.Add(sidebarColumn);
            mainGrid.ColumnDefinitions.Add(contentColumn);
        }
        else
        {
            mainGrid.ColumnDefinitions.Add(contentColumn);
            mainGrid.ColumnDefinitions.Add(sidebarColumn);
        }

        // 创建侧边栏
        var sidebarControl = CreateSidebar(context);
        
        // 设置侧边栏位置
        Grid.SetColumn(sidebarControl, SidebarPosition == NavigationSidebarPosition.Left ? 0 : 1);

        // 创建内容区域
        var contentControl = CreateContentArea(context);
        Grid.SetColumn(contentControl, SidebarPosition == NavigationSidebarPosition.Left ? 1 : 0);

        mainGrid.Children.Add(sidebarControl);
        mainGrid.Children.Add(contentControl);

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(mainGrid, this);

        return mainGrid;
    }

    /// <summary>
    /// 创建侧边栏
    /// </summary>
    private Control CreateSidebar(BuildContext context)
    {
        var sidebar = new Border
        {
            Background = GetSidebarBrush()
        };

        var sidebarStack = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Spacing = 2
        };

        // 添加标题
        if (ShowHeader && !string.IsNullOrEmpty(HeaderTitle))
        {
            var titleText = new TextBlock
            {
                Text = HeaderTitle,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(16, 16, 16, 8),
                Foreground = GetTextBrush()
            };

            sidebarStack.Children.Add(titleText);
        }

        // 添加菜单项
        foreach (var menuItem in MenuItems)
        {
            var menuItemControl = CreateMenuItem(menuItem, context);
            sidebarStack.Children.Add(menuItemControl);
        }

        sidebar.Child = sidebarStack;
        return sidebar;
    }

    /// <summary>
    /// 创建菜单项
    /// </summary>
    private Control CreateMenuItem(NavigationItem item, BuildContext context)
    {
        var button = new Button
        {
            Content = CreateMenuItemContent(item),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Background = null,
            BorderThickness = new Avalonia.Thickness(0),
            Padding = new Avalonia.Thickness(16, 12, 16, 12),
            Margin = new Avalonia.Thickness(0),
            Tag = item.Value
        };

        // 设置按钮样式
        button.Foreground = GetTextBrush();
        button.FontSize = 14;

        // 设置选中状态样式
        if (item.Value == SelectedValue)
        {
            button.Background = GetSelectedItemBrush();
        }

        // 注册点击事件
        button.Click += (sender, e) =>
        {
            try
            {
                SelectedValue = item.Value;
                OnSelectionChanged?.Invoke(item.Value);
                
                // 触发界面更新（这里需要依赖状态管理机制）
                // 在实际使用中，应该通过状态管理来更新UI
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NavigationView] Error in menu item click: {ex.Message}");
            }
        };

        // 添加悬停效果
        button.PointerEntered += (sender, e) =>
        {
            if (item.Value != SelectedValue)
            {
                button.Background = GetHoverItemBrush();
            }
        };

        button.PointerExited += (sender, e) =>
        {
            if (item.Value != SelectedValue)
            {
                button.Background = null;
            }
        };

        return button;
    }

    /// <summary>
    /// 创建菜单项内容
    /// </summary>
    private object CreateMenuItemContent(NavigationItem item)
    {
        if (DisplayMode == NavigationDisplayMode.Compact)
        {
            // 紧凑模式只显示图标
            return !string.IsNullOrEmpty(item.Icon) 
                ? new TextBlock { Text = item.Icon, FontSize = 20 }
                : new TextBlock { Text = item.Label.Substring(0, 1).ToUpper(), FontSize = 16 };
        }
        else
        {
            // 展开模式显示图标和文本
            var stackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 12
            };

            if (!string.IsNullOrEmpty(item.Icon))
            {
                var iconText = new TextBlock 
                { 
                    Text = item.Icon, 
                    FontSize = 18,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                stackPanel.Children.Add(iconText);
            }

            var labelText = new TextBlock 
            { 
                Text = item.Label, 
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            stackPanel.Children.Add(labelText);

            return stackPanel;
        }
    }

    /// <summary>
    /// 创建内容区域
    /// </summary>
    private Control CreateContentArea(BuildContext context)
    {
        var border = new Border
        {
            Background = Brushes.White,
            Padding = new Avalonia.Thickness(16)
        };

        if (Content != null)
        {
            var contentControl = Content.Build(context);
            if (contentControl is Control control)
            {
                border.Child = control;
            }
        }

        return border;
    }

    /// <summary>
    /// 获取侧边栏画刷
    /// </summary>
    private IBrush GetSidebarBrush()
    {
        if (!string.IsNullOrEmpty(SidebarBackgroundColor))
        {
            return LayoutHelper.ParseColorBrush(SidebarBackgroundColor);
        }
        return new SolidColorBrush(Color.FromRgb(245, 245, 245)); // 默认浅灰色
    }

    /// <summary>
    /// 获取文本画刷
    /// </summary>
    private IBrush GetTextBrush()
    {
        if (!string.IsNullOrEmpty(TextColor))
        {
            return LayoutHelper.ParseColorBrush(TextColor);
        }
        return Brushes.Black; // 默认黑色
    }

    /// <summary>
    /// 获取选中项画刷
    /// </summary>
    private IBrush GetSelectedItemBrush()
    {
        if (!string.IsNullOrEmpty(SelectedItemBackgroundColor))
        {
            return LayoutHelper.ParseColorBrush(SelectedItemBackgroundColor);
        }
        return new SolidColorBrush(Color.FromRgb(0, 122, 255)); // 默认蓝色
    }

    /// <summary>
    /// 获取悬停项画刷
    /// </summary>
    private IBrush GetHoverItemBrush()
    {
        if (!string.IsNullOrEmpty(HoverItemBackgroundColor))
        {
            return LayoutHelper.ParseColorBrush(HoverItemBackgroundColor);
        }
        return new SolidColorBrush(Color.FromRgb(220, 220, 220)); // 默认浅灰色
    }

    /// <summary>
    /// 链式调用：设置菜单项
    /// </summary>
    public NavigationView SetMenuItems(List<NavigationItem> items)
    {
        MenuItems = items ?? [];
        return this;
    }

    /// <summary>
    /// 链式调用：添加菜单项
    /// </summary>
    public NavigationView AddMenuItem(string label, string value, string? icon = null)
    {
        MenuItems.Add(new NavigationItem { Label = label, Value = value, Icon = icon });
        return this;
    }

    /// <summary>
    /// 链式调用：设置选中值
    /// </summary>
    public NavigationView SetSelectedValue(string? value)
    {
        SelectedValue = value;
        return this;
    }

    /// <summary>
    /// 链式调用：设置选择变化回调
    /// </summary>
    public NavigationView SetOnSelectionChanged(Action<string?> onChanged)
    {
        OnSelectionChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置内容区域
    /// </summary>
    public NavigationView SetContent(WidgetBase content)
    {
        Content = content;
        return this;
    }

    /// <summary>
    /// 链式调用：设置侧边栏宽度
    /// </summary>
    public NavigationView SetSidebarWidth(double width)
    {
        SidebarWidth = width;
        return this;
    }

    /// <summary>
    /// 链式调用：设置显示模式
    /// </summary>
    public NavigationView SetDisplayMode(NavigationDisplayMode mode)
    {
        DisplayMode = mode;
        return this;
    }

    /// <summary>
    /// 链式调用：设置侧边栏位置
    /// </summary>
    public NavigationView SetSidebarPosition(NavigationSidebarPosition position)
    {
        SidebarPosition = position;
        return this;
    }

    /// <summary>
    /// 链式调用：设置颜色主题
    /// </summary>
    public NavigationView SetColors(string? sidebarBg = null, string? selectedItemBg = null, 
        string? hoverItemBg = null, string? text = null)
    {
        SidebarBackgroundColor = sidebarBg;
        SelectedItemBackgroundColor = selectedItemBg;
        HoverItemBackgroundColor = hoverItemBg;
        TextColor = text;
        return this;
    }

    /// <summary>
    /// 链式调用：设置标题
    /// </summary>
    public NavigationView SetHeader(string title, bool show = true)
    {
        HeaderTitle = title;
        ShowHeader = show;
        return this;
    }
}

/// <summary>
/// 导航项
/// </summary>
public class NavigationItem
{
    /// <summary>
    /// 显示标签
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 项的值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 图标（可选）
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 子项（用于多级菜单）
    /// </summary>
    public List<NavigationItem>? Children { get; set; }

    public NavigationItem()
    {
    }

    public NavigationItem(string label, string value, string? icon = null, bool isDisabled = false)
    {
        Label = label;
        Value = value;
        Icon = icon;
        IsDisabled = isDisabled;
    }
}

/// <summary>
/// 导航侧边栏位置
/// </summary>
public enum NavigationSidebarPosition
{
    /// <summary>
    /// 左侧
    /// </summary>
    Left,

    /// <summary>
    /// 右侧
    /// </summary>
    Right
}

/// <summary>
/// 导航显示模式
/// </summary>
public enum NavigationDisplayMode
{
    /// <summary>
    /// 展开模式
    /// </summary>
    Expanded,

    /// <summary>
    /// 紧凑模式
    /// </summary>
    Compact
}