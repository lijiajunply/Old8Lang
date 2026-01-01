using Avalonia.Controls;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Panel 面板组件
/// 提供带标题栏和边框的容器
/// </summary>
public class Panel(WidgetBase? content = null) : WidgetBase
{
    /// <summary>
    /// 面板内容
    /// </summary>
    public WidgetBase? Content { get; set; } = content;

    /// <summary>
    /// 面板标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 副标题
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// 标题栏操作按钮
    /// </summary>
    public List<PanelAction> Actions { get; set; } = [];

    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; } = 1;

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 边框圆角
    /// </summary>
    public double CornerRadius { get; set; } = 8;

    /// <summary>
    /// 阴影效果
    /// </summary>
    public PanelShadow Shadow { get; set; } = PanelShadow.Small;

    /// <summary>
    /// 标题栏背景色
    /// </summary>
    public string? HeaderBackgroundColor { get; set; }

    /// <summary>
    /// 内容区域背景色
    /// </summary>
    public string? ContentBackgroundColor { get; set; }

    /// <summary>
    /// 是否显示标题栏
    /// </summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// 是否可折叠
    /// </summary>
    public bool Collapsible { get; set; } = false;

    /// <summary>
    /// 是否展开状态
    /// </summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// 折叠状态变化回调
    /// </summary>
    public Action<bool>? OnCollapsedChanged { get; set; }

    /// <summary>
    /// 标题栏高度
    /// </summary>
    public double HeaderHeight { get; set; } = 48;

    /// <summary>
    /// 内容区域内边距
    /// </summary>
    public Thickness ContentPadding { get; set; } = new(16, 16, 16, 16);

    public override object Build(BuildContext context)
    {
        var mainBorder = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(CornerRadius),
            BorderThickness = new Avalonia.Thickness(BorderWidth)
        };

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(mainBorder, this);

        // 设置边框颜色
        if (!string.IsNullOrEmpty(BorderColor))
        {
            mainBorder.BorderBrush = LayoutHelper.ParseColorBrush(BorderColor);
        }

        // 设置阴影
        ApplyShadow(mainBorder);

        var mainStack = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical
        };

        // 添加标题栏
        if (ShowHeader)
        {
            var headerControl = CreateHeader(context);
            mainStack.Children.Add(headerControl);
        }

        // 添加内容区域
        if (Content != null)
        {
            var contentControl = CreateContentArea(context);
            mainStack.Children.Add(contentControl);
        }

        mainBorder.Child = mainStack;

        return mainBorder;
    }

    /// <summary>
    /// 创建标题栏
    /// </summary>
    private Control CreateHeader(BuildContext context)
    {
        var headerBorder = new Border
        {
            Height = HeaderHeight,
            CornerRadius = new Avalonia.CornerRadius(CornerRadius, CornerRadius, 0, 0)
        };

        // 设置标题栏背景色
        if (!string.IsNullOrEmpty(HeaderBackgroundColor))
        {
            headerBorder.Background = LayoutHelper.ParseColorBrush(HeaderBackgroundColor);
        }

        var headerGrid = new Grid();

        // 定义列：标题区域 + 操作按钮区域
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // 标题
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });   // 操作按钮

        // 创建标题区域
        var titleStack = CreateTitleArea();
        Grid.SetColumn(titleStack, 0);

        // 创建操作按钮区域
        var actionsPanel = CreateActionsArea(context);
        Grid.SetColumn(actionsPanel, 1);

        // 如果可折叠，添加折叠按钮
        if (Collapsible)
        {
            var collapseButton = CreateCollapseButton();
            Grid.SetColumn(collapseButton, 1);
            headerGrid.Children.Add(collapseButton);
        }

        headerGrid.Children.Add(titleStack);
        headerGrid.Children.Add(actionsPanel);

        headerBorder.Child = headerGrid;

        return headerBorder;
    }

    /// <summary>
    /// 创建标题区域
    /// </summary>
    private Control CreateTitleArea()
    {
        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(16, 0, 16, 0)
        };

        if (!string.IsNullOrEmpty(Title))
        {
            var titleText = new TextBlock
            {
                Text = Title,
                FontSize = 16,
                FontWeight = FontWeight.SemiBold
            };
            stackPanel.Children.Add(titleText);
        }

        if (!string.IsNullOrEmpty(Subtitle))
        {
            var subtitleText = new TextBlock
            {
                Text = Subtitle,
                FontSize = 12,
                Opacity = 0.7
            };
            stackPanel.Children.Add(subtitleText);
        }

        return stackPanel;
    }

    /// <summary>
    /// 创建操作按钮区域
    /// </summary>
    private Control CreateActionsArea(BuildContext context)
    {
        if (Actions.Count == 0)
        {
            return new Control();
        }

        var stackPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 16, 0)
        };

        foreach (var action in Actions)
        {
            var button = new Avalonia.Controls.Button
            {
                Content = action.Label,
                IsEnabled = !action.IsDisabled
            };

            // 设置按钮样式
            if (!string.IsNullOrEmpty(action.BackgroundColor))
            {
                button.Background = LayoutHelper.ParseColorBrush(action.BackgroundColor);
            }

            if (!string.IsNullOrEmpty(action.TextColor))
            {
                button.Foreground = LayoutHelper.ParseColorBrush(action.TextColor);
            }

            // 注册点击事件
            if (action.OnClick != null)
            {
                button.Click += (sender, e) =>
                {
                    try
                    {
                        action.OnClick.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Panel] Error in action click: {ex.Message}");
                    }
                };
            }

            stackPanel.Children.Add(button);
        }

        return stackPanel;
    }

    /// <summary>
    /// 创建折叠按钮
    /// </summary>
    private Control CreateCollapseButton()
    {
        var button = new Avalonia.Controls.Button
        {
            Content = IsExpanded ? "▼" : "▶",
            Width = 32,
            Height = 32,
            Margin = new Avalonia.Thickness(0, 0, 8, 0),
            Background = null,
            BorderThickness = new Avalonia.Thickness(0)
        };

        button.Click += (sender, e) =>
        {
            try
            {
                IsExpanded = !IsExpanded;
                OnCollapsedChanged?.Invoke(IsExpanded);
                // 这里需要实现内容区域的显示/隐藏逻辑
                // 在实际使用中，应该通过状态管理来更新UI
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Panel] Error in collapse button: {ex.Message}");
            }
        };

        return button;
    }

    /// <summary>
    /// 创建内容区域
    /// </summary>
    private Control CreateContentArea(BuildContext context)
    {
        var contentBorder = new Border
        {
            Padding = new Avalonia.Thickness(
                ContentPadding.Left,
                ContentPadding.Top,
                ContentPadding.Right,
                ContentPadding.Bottom)
        };

        // 设置内容区域背景色
        if (!string.IsNullOrEmpty(ContentBackgroundColor))
        {
            contentBorder.Background = LayoutHelper.ParseColorBrush(ContentBackgroundColor);
        }
        else
        {
            // 内容区域无背景
            contentBorder.Background = null;
        }

        // 设置内容区域圆角（只有底部）
        if (ShowHeader)
        {
            contentBorder.CornerRadius = new Avalonia.CornerRadius(0, 0, CornerRadius, CornerRadius);
        }
        else
        {
            contentBorder.CornerRadius = new Avalonia.CornerRadius(CornerRadius);
        }

        // 构建内容
        var contentControl = Content.Build(context);
        if (contentControl is Control control)
        {
            contentBorder.Child = control;
        }

        // 如果可折叠且未展开，隐藏内容
        if (Collapsible && !IsExpanded)
        {
            contentBorder.IsVisible = false;
        }

        return contentBorder;
    }

    /// <summary>
    /// 应用阴影效果
    /// </summary>
    private void ApplyShadow(Border border)
    {
        // 简单的阴影实现，实际可以使用 DropShadowEffect
        if (Shadow != PanelShadow.None)
        {
            // 这里可以通过设置 Margin 来模拟阴影效果
            var shadowMargin = Shadow switch
            {
                PanelShadow.Small => 2,
                PanelShadow.Medium => 4,
                PanelShadow.Large => 8,
                _ => 0
            };
            
            border.Margin = new Avalonia.Thickness(shadowMargin);
        }
    }

    /// <summary>
    /// 链式调用：设置内容
    /// </summary>
    public Panel SetContent(WidgetBase content)
    {
        Content = content;
        return this;
    }

    /// <summary>
    /// 链式调用：设置标题
    /// </summary>
    public Panel SetTitle(string title, string? subtitle = null)
    {
        Title = title;
        Subtitle = subtitle;
        return this;
    }

    /// <summary>
    /// 链式调用：添加操作按钮
    /// </summary>
    public Panel AddAction(string label, Action onClick, bool isDisabled = false, 
        string? backgroundColor = null, string? textColor = null)
    {
        Actions.Add(new PanelAction
        {
            Label = label,
            OnClick = onClick,
            IsDisabled = isDisabled,
            BackgroundColor = backgroundColor,
            TextColor = textColor
        });
        return this;
    }

    /// <summary>
    /// 链式调用：设置边框
    /// </summary>
    public Panel SetBorder(string color, double width = 1, double cornerRadius = 8)
    {
        BorderColor = color;
        BorderWidth = width;
        CornerRadius = cornerRadius;
        return this;
    }

    /// <summary>
    /// 链式调用：设置阴影
    /// </summary>
    public Panel SetShadow(PanelShadow shadow)
    {
        Shadow = shadow;
        return this;
    }

    /// <summary>
    /// 链式调用：设置颜色
    /// </summary>
    public Panel SetColors(string? borderColor = null, string? headerBg = null, 
        string? contentBg = null)
    {
        BorderColor = borderColor;
        HeaderBackgroundColor = headerBg;
        ContentBackgroundColor = contentBg;
        return this;
    }

    /// <summary>
    /// 链式调用：设置折叠功能
    /// </summary>
    public Panel SetCollapsible(bool collapsible = true, bool isExpanded = true, 
        Action<bool>? onCollapsedChanged = null)
    {
        Collapsible = collapsible;
        IsExpanded = isExpanded;
        OnCollapsedChanged = onCollapsedChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置尺寸
    /// </summary>
    public Panel SetSizes(double headerHeight = 48, Thickness? contentPadding = null)
    {
        HeaderHeight = headerHeight;
        if (contentPadding.HasValue)
        {
            ContentPadding = contentPadding.Value;
        }
        return this;
    }
}

/// <summary>
/// 面板操作按钮
/// </summary>
public class PanelAction
{
    /// <summary>
    /// 按钮标签
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 点击事件
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// 文本颜色
    /// </summary>
    public string? TextColor { get; set; }
}

/// <summary>
/// 面板阴影效果
/// </summary>
public enum PanelShadow
{
    /// <summary>
    /// 无阴影
    /// </summary>
    None,

    /// <summary>
    /// 小阴影
    /// </summary>
    Small,

    /// <summary>
    /// 中等阴影
    /// </summary>
    Medium,

    /// <summary>
    /// 大阴影
    /// </summary>
    Large
}