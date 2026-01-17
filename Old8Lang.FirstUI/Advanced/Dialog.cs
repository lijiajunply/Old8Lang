using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Dialog 对话框组件
/// </summary>
public class Dialog : WidgetBase
{
    /// <summary>
    /// 对话框标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 对话框内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 内容组件
    /// </summary>
    public WidgetBase? ContentWidget { get; set; }

    /// <summary>
    /// 对话框宽度
    /// </summary>
    public double DialogWidth { get; set; } = 400;

    /// <summary>
    /// 对话框高度（0 表示自动）
    /// </summary>
    public double DialogHeight { get; set; } = 0;

    /// <summary>
    /// 确认按钮文本
    /// </summary>
    public string ConfirmText { get; set; } = "确认";

    /// <summary>
    /// 取消按钮文本
    /// </summary>
    public string CancelText { get; set; } = "取消";

    /// <summary>
    /// 是否显示取消按钮
    /// </summary>
    public bool ShowCancelButton { get; set; } = true;

    /// <summary>
    /// 确认回调
    /// </summary>
    public Action? OnConfirm { get; set; }

    /// <summary>
    /// 取消回调
    /// </summary>
    public Action? OnCancel { get; set; }

    /// <summary>
    /// 是否显示关闭按钮
    /// </summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// 对话框类型
    /// </summary>
    public DialogType Type { get; set; } = DialogType.Default;

    public override object Build(BuildContext context)
    {
        // 创建遮罩层
        var overlay = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // 创建对话框容器
        var dialogBorder = new Border
        {
            Width = DialogWidth,
            Background = Utils.LayoutHelper.ParseColorBrush("#FFFFFF"),
            CornerRadius = new CornerRadius(8),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 16,
                Spread = 0,
                OffsetX = 0,
                OffsetY = 8,
                Color = Color.FromArgb(80, 0, 0, 0)
            }),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (DialogHeight > 0)
        {
            dialogBorder.Height = DialogHeight;
        }

        // 创建对话框内容
        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        // 标题栏
        if (!string.IsNullOrEmpty(Title))
        {
            var titleBar = new Grid
            {
                Height = 50,
                Background = GetTypeColor()
            };

            var titleText = new TextBlock
            {
                Text = Title,
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
                Foreground = Utils.LayoutHelper.ParseColorBrush("#FFFFFF"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(16, 0)
            };
            titleBar.Children.Add(titleText);

            // 关闭按钮
            if (ShowCloseButton)
            {
                var closeButton = new Button
                {
                    Content = "✕",
                    FontSize = 18,
                    Width = 32,
                    Height = 32,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),
                    Foreground = Utils.LayoutHelper.ParseColorBrush("#FFFFFF"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 8, 0)
                };
                closeButton.Click += (s, e) => OnCancel?.Invoke();
                titleBar.Children.Add(closeButton);
            }

            contentPanel.Children.Add(titleBar);
        }

        // 内容区域
        var contentArea = new Border
        {
            Padding = new Avalonia.Thickness(16),
            MinHeight = 80
        };

        if (ContentWidget != null)
        {
            var widget = ContentWidget.Build(context);
            if (widget is Control control)
            {
                contentArea.Child = control;
            }
        }
        else if (!string.IsNullOrEmpty(Content))
        {
            contentArea.Child = new TextBlock
            {
                Text = Content,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };
        }

        contentPanel.Children.Add(contentArea);

        // 按钮区域
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Avalonia.Thickness(16, 0, 16, 16)
        };

        if (ShowCancelButton)
        {
            var cancelButton = new Button
            {
                Content = CancelText,
                Padding = new Avalonia.Thickness(20, 8),
                Background = Utils.LayoutHelper.ParseColorBrush("#F5F5F5"),
                Foreground = Utils.LayoutHelper.ParseColorBrush("#333333"),
                BorderThickness = new Avalonia.Thickness(1),
                BorderBrush = Utils.LayoutHelper.ParseColorBrush("#E0E0E0"),
                CornerRadius = new CornerRadius(4)
            };
            cancelButton.Click += (s, e) => OnCancel?.Invoke();
            buttonPanel.Children.Add(cancelButton);
        }

        var confirmButton = new Button
        {
            Content = ConfirmText,
            Padding = new Avalonia.Thickness(20, 8),
            Background = GetTypeColor(),
            Foreground = Utils.LayoutHelper.ParseColorBrush("#FFFFFF"),
            BorderThickness = new Avalonia.Thickness(0),
            CornerRadius = new CornerRadius(4)
        };
        confirmButton.Click += (s, e) => OnConfirm?.Invoke();
        buttonPanel.Children.Add(confirmButton);

        contentPanel.Children.Add(buttonPanel);

        dialogBorder.Child = contentPanel;
        overlay.Children.Add(dialogBorder);

        return overlay;
    }

    private IBrush GetTypeColor()
    {
        return Type switch
        {
            DialogType.Success => Utils.LayoutHelper.ParseColorBrush("#4CAF50"),
            DialogType.Warning => Utils.LayoutHelper.ParseColorBrush("#FF9800"),
            DialogType.Error => Utils.LayoutHelper.ParseColorBrush("#F44336"),
            DialogType.Info => Utils.LayoutHelper.ParseColorBrush("#2196F3"),
            _ => Utils.LayoutHelper.ParseColorBrush("#2196F3")
        };
    }

    /// <summary>
    /// 链式调用：设置标题
    /// </summary>
    public Dialog SetTitle(string title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    /// 链式调用：设置内容
    /// </summary>
    public Dialog SetContent(string content)
    {
        Content = content;
        return this;
    }

    /// <summary>
    /// 链式调用：设置内容组件
    /// </summary>
    public Dialog SetContentWidget(WidgetBase widget)
    {
        ContentWidget = widget;
        return this;
    }

    /// <summary>
    /// 链式调用：设置对话框大小
    /// </summary>
    public Dialog SetDialogSize(double width, double height = 0)
    {
        DialogWidth = width;
        DialogHeight = height;
        return this;
    }

    /// <summary>
    /// 链式调用：设置按钮文本
    /// </summary>
    public Dialog SetButtonText(string confirmText, string? cancelText = null)
    {
        ConfirmText = confirmText;
        if (cancelText != null)
            CancelText = cancelText;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示取消按钮
    /// </summary>
    public Dialog SetShowCancelButton(bool show)
    {
        ShowCancelButton = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置确认回调
    /// </summary>
    public Dialog SetOnConfirm(Action onConfirm)
    {
        OnConfirm = onConfirm;
        return this;
    }

    /// <summary>
    /// 链式调用：设置取消回调
    /// </summary>
    public Dialog SetOnCancel(Action onCancel)
    {
        OnCancel = onCancel;
        return this;
    }

    /// <summary>
    /// 链式调用：设置对话框类型
    /// </summary>
    public Dialog SetType(DialogType type)
    {
        Type = type;
        return this;
    }
}

/// <summary>
/// 对话框类型枚举
/// </summary>
public enum DialogType
{
    Default,
    Success,
    Warning,
    Error,
    Info
}
