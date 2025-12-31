using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// Button 按钮组件
/// </summary>
public class Button : WidgetBase
{
    /// <summary>
    /// 按钮文本标签
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// 点击事件回调
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// 按钮图标（可选）
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 按钮样式变体
    /// </summary>
    public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 文本颜色
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// 圆角
    /// </summary>
    public double BorderRadius { get; set; } = 4;

    public Button(string label, Action? onClick = null)
    {
        Label = label;
        OnClick = onClick;
    }

    public override object Build(BuildContext context)
    {
        var button = new Avalonia.Controls.Button
        {
            Content = Label,
            IsEnabled = !IsDisabled,
            Cursor = new Cursor(StandardCursorType.Hand),
            CornerRadius = new Avalonia.CornerRadius(BorderRadius)
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(button, this);

        // 设置默认内边距（如果未设置）
        if (Padding.Left == 0 && Padding.Top == 0 && Padding.Right == 0 && Padding.Bottom == 0)
        {
            button.Padding = new Avalonia.Thickness(16, 8);
        }
        else
        {
            button.Padding = new Avalonia.Thickness(
                Padding.Left,
                Padding.Top,
                Padding.Right,
                Padding.Bottom
            );
        }

        // 根据变体设置样式
        ApplyVariantStyles(button);

        // 设置文本颜色
        if (!string.IsNullOrEmpty(TextColor))
        {
            button.Foreground = LayoutHelper.ParseColorBrush(TextColor);
        }

        // 设置字体大小
        button.FontSize = FontSize;

        // 注册点击事件
        if (OnClick != null)
        {
            button.Click += (sender, e) =>
            {
                try
                {
                    OnClick.Invoke();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Button] Error in click handler: {ex.Message}");
                }
            };
        }

        return button;
    }

    /// <summary>
    /// 根据变体应用样式
    /// </summary>
    private void ApplyVariantStyles(Avalonia.Controls.Button button)
    {
        // 如果用户设置了背景色，则优先使用用户设置
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            button.Background = LayoutHelper.ParseColorBrush(BackgroundColor);
            return;
        }

        // 否则根据变体设置默认样式
        var (bgColor, fgColor) = Variant switch
        {
            ButtonVariant.Primary => ("#007AFF", "#FFFFFF"),
            ButtonVariant.Secondary => ("#5856D6", "#FFFFFF"),
            ButtonVariant.Success => ("#34C759", "#FFFFFF"),
            ButtonVariant.Danger => ("#FF3B30", "#FFFFFF"),
            ButtonVariant.Warning => ("#FF9500", "#FFFFFF"),
            ButtonVariant.Info => ("#5AC8FA", "#FFFFFF"),
            ButtonVariant.Light => ("#F2F2F7", "#000000"),
            ButtonVariant.Dark => ("#1C1C1E", "#FFFFFF"),
            ButtonVariant.Outline => ("Transparent", "#007AFF"),
            _ => ("#007AFF", "#FFFFFF")
        };

        button.Background = LayoutHelper.ParseColorBrush(bgColor);

        if (string.IsNullOrEmpty(TextColor))
        {
            button.Foreground = LayoutHelper.ParseColorBrush(fgColor);
        }

        // Outline 变体需要边框
        if (Variant == ButtonVariant.Outline)
        {
            button.BorderBrush = LayoutHelper.ParseColorBrush("#007AFF");
            button.BorderThickness = new Avalonia.Thickness(1);
        }
    }

    /// <summary>
    /// 链式调用：设置点击事件
    /// </summary>
    public Button SetOnClick(Action onClick)
    {
        OnClick = onClick;
        return this;
    }

    /// <summary>
    /// 链式调用：设置按钮变体
    /// </summary>
    public Button SetVariant(ButtonVariant variant)
    {
        Variant = variant;
        return this;
    }

    /// <summary>
    /// 链式调用：设置禁用状态
    /// </summary>
    public Button SetDisabled(bool disabled)
    {
        IsDisabled = disabled;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本颜色
    /// </summary>
    public Button SetTextColor(string color)
    {
        TextColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public Button SetFontSize(double size)
    {
        FontSize = size;
        return this;
    }

    /// <summary>
    /// 链式调用：设置圆角
    /// </summary>
    public Button SetBorderRadius(double radius)
    {
        BorderRadius = radius;
        return this;
    }
}

/// <summary>
/// 按钮样式变体
/// </summary>
public enum ButtonVariant
{
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info,
    Light,
    Dark,
    Outline
}
