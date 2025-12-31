using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Advanced;

/// <summary>
/// Toast 消息提示组件
/// </summary>
public class Toast : WidgetBase
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// 显示持续时间（毫秒）
    /// </summary>
    public int Duration { get; set; } = 3000;

    /// <summary>
    /// 消息类型
    /// </summary>
    public ToastType Type { get; set; } = ToastType.Info;

    /// <summary>
    /// 位置
    /// </summary>
    public ToastPosition Position { get; set; } = ToastPosition.Top;

    /// <summary>
    /// 是否显示图标
    /// </summary>
    public bool ShowIcon { get; set; } = true;

    /// <summary>
    /// 关闭回调
    /// </summary>
    public Action? OnClose { get; set; }

    public override object Build(BuildContext context)
    {
        // 创建容器
        var container = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = GetVerticalAlignment(),
            Margin = new Avalonia.Thickness(0, GetTopMargin(), 0, GetBottomMargin())
        };

        // 创建 Toast 内容
        var toastBorder = new Border
        {
            Background = GetBackgroundColor(),
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(16, 12),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 8,
                Spread = 0,
                OffsetX = 0,
                OffsetY = 2,
                Color = Color.FromArgb(60, 0, 0, 0)
            }),
            MinWidth = 200,
            MaxWidth = 400
        };

        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        // 添加图标
        if (ShowIcon)
        {
            var icon = new TextBlock
            {
                Text = GetIcon(),
                FontSize = 16,
                Foreground = Utils.LayoutHelper.ParseColorBrush("#FFFFFF"),
                VerticalAlignment = VerticalAlignment.Center
            };
            contentPanel.Children.Add(icon);
        }

        // 添加消息文本
        var messageText = new TextBlock
        {
            Text = Message,
            FontSize = 14,
            Foreground = Utils.LayoutHelper.ParseColorBrush("#FFFFFF"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        contentPanel.Children.Add(messageText);

        toastBorder.Child = contentPanel;
        container.Children.Add(toastBorder);

        // 自动关闭
        if (Duration > 0)
        {
            DispatcherTimer.RunOnce(() =>
            {
                OnClose?.Invoke();
            }, TimeSpan.FromMilliseconds(Duration));
        }

        return container;
    }

    private VerticalAlignment GetVerticalAlignment()
    {
        return Position switch
        {
            ToastPosition.Top => VerticalAlignment.Top,
            ToastPosition.Center => VerticalAlignment.Center,
            ToastPosition.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Top
        };
    }

    private double GetTopMargin()
    {
        return Position == ToastPosition.Top ? 20 : 0;
    }

    private double GetBottomMargin()
    {
        return Position == ToastPosition.Bottom ? 20 : 0;
    }

    private IBrush GetBackgroundColor()
    {
        return Type switch
        {
            ToastType.Success => Utils.LayoutHelper.ParseColorBrush("#4CAF50"),
            ToastType.Warning => Utils.LayoutHelper.ParseColorBrush("#FF9800"),
            ToastType.Error => Utils.LayoutHelper.ParseColorBrush("#F44336"),
            ToastType.Info => Utils.LayoutHelper.ParseColorBrush("#2196F3"),
            _ => Utils.LayoutHelper.ParseColorBrush("#333333")
        };
    }

    private string GetIcon()
    {
        return Type switch
        {
            ToastType.Success => "✓",
            ToastType.Warning => "⚠",
            ToastType.Error => "✕",
            ToastType.Info => "ℹ",
            _ => "ℹ"
        };
    }

    /// <summary>
    /// 链式调用：设置消息内容
    /// </summary>
    public Toast SetMessage(string message)
    {
        Message = message;
        return this;
    }

    /// <summary>
    /// 链式调用：设置持续时间
    /// </summary>
    public Toast SetDuration(int duration)
    {
        Duration = duration;
        return this;
    }

    /// <summary>
    /// 链式调用：设置消息类型
    /// </summary>
    public Toast SetType(ToastType type)
    {
        Type = type;
        return this;
    }

    /// <summary>
    /// 链式调用：设置位置
    /// </summary>
    public Toast SetPosition(ToastPosition position)
    {
        Position = position;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否显示图标
    /// </summary>
    public Toast SetShowIcon(bool show)
    {
        ShowIcon = show;
        return this;
    }

    /// <summary>
    /// 链式调用：设置关闭回调
    /// </summary>
    public Toast SetOnClose(Action onClose)
    {
        OnClose = onClose;
        return this;
    }

    /// <summary>
    /// 静态方法：显示成功提示
    /// </summary>
    public static Toast Success(string message, int duration = 3000)
    {
        return new Toast
        {
            Message = message,
            Type = ToastType.Success,
            Duration = duration
        };
    }

    /// <summary>
    /// 静态方法：显示警告提示
    /// </summary>
    public static Toast Warning(string message, int duration = 3000)
    {
        return new Toast
        {
            Message = message,
            Type = ToastType.Warning,
            Duration = duration
        };
    }

    /// <summary>
    /// 静态方法：显示错误提示
    /// </summary>
    public static Toast Error(string message, int duration = 3000)
    {
        return new Toast
        {
            Message = message,
            Type = ToastType.Error,
            Duration = duration
        };
    }

    /// <summary>
    /// 静态方法：显示信息提示
    /// </summary>
    public static Toast Info(string message, int duration = 3000)
    {
        return new Toast
        {
            Message = message,
            Type = ToastType.Info,
            Duration = duration
        };
    }
}

/// <summary>
/// Toast 类型枚举
/// </summary>
public enum ToastType
{
    Success,
    Warning,
    Error,
    Info
}

/// <summary>
/// Toast 位置枚举
/// </summary>
public enum ToastPosition
{
    Top,
    Center,
    Bottom
}
