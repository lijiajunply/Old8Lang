using Avalonia.Controls;
using Avalonia.Media;
using FirstUI.Core;
using FirstUI.Utils;

namespace FirstUI.Basic;

/// <summary>
/// TextArea 多行文本输入组件
/// 支持多行文本编辑、自动换行等功能
/// </summary>
public class TextArea(string? value = null) : WidgetBase
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Value { get; set; } = value ?? string.Empty;

    /// <summary>
    /// 占位符文本
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// 文本变化回调
    /// </summary>
    public Action<string>? OnChanged { get; set; }

    /// <summary>
    /// 焦点获得回调
    /// </summary>
    public Action? OnFocus { get; set; }

    /// <summary>
    /// 焦点失去回调
    /// </summary>
    public Action? OnBlur { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 是否自动换行
    /// </summary>
    public bool AutoWrap { get; set; } = true;

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// 字体粗细
    /// </summary>
    public string FontWeight { get; set; } = "normal";

    /// <summary>
    /// 文本颜色
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColorOverride { get; set; }

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; } = 1;

    /// <summary>
    /// 圆角半径
    /// </summary>
    public double CornerRadius { get; set; } = 4;

    /// <summary>
    /// 内边距
    /// </summary>
    public Thickness TextPadding { get; set; } = new(8, 8, 8, 8);

    /// <summary>
    /// 最小行数
    /// </summary>
    public int MinLines { get; set; } = 3;

    /// <summary>
    /// 最大行数
    /// </summary>
    public int MaxLines { get; set; } = 10;

    /// <summary>
    /// 是否显示水平滚动条
    /// </summary>
    public bool ShowHorizontalScrollBar { get; set; } = false;

    /// <summary>
    /// 是否显示垂直滚动条
    /// </summary>
    public bool ShowVerticalScrollBar { get; set; } = true;

    /// <summary>
    /// 文本对齐方式
    /// </summary>
    public Avalonia.Media.TextAlignment TextAlignment { get; set; } = Avalonia.Media.TextAlignment.Left;

    /// <summary>
    /// 字体样式
    /// </summary>
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;

    /// <summary>
    /// 最大字符长度
    /// </summary>
    public int MaxLength { get; set; } = -1; // -1 表示无限制

    public override object Build(BuildContext context)
    {
        var textBlock = new TextBox
        {
            Text = Value,
            Watermark = Placeholder,
            IsReadOnly = IsReadOnly,
            IsEnabled = !IsDisabled,
            TextWrapping = AutoWrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            FontSize = FontSize,
            MinLines = MinLines,
            MaxLines = MaxLines,
            TextAlignment = TextAlignment,
            AcceptsReturn = true, // 支持回车换行
            AcceptsTab = true     // 支持Tab键
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(textBlock, this);

        // 设置字体粗细
        textBlock.FontWeight = ParseFontWeight(FontWeight);

        // 设置字体样式
        textBlock.FontStyle = FontStyle;

        // 设置内边距
        textBlock.Padding = new Avalonia.Thickness(
            TextPadding.Left,
            TextPadding.Top,
            TextPadding.Right,
            TextPadding.Bottom
        );

        // 应用自定义样式
        ApplyTextAreaStyles(textBlock);

        // 设置最大长度
        if (MaxLength > 0)
        {
            textBlock.MaxLength = MaxLength;
        }

        // 注册事件
        RegisterEvents(textBlock);

        return textBlock;
    }

    /// <summary>
    /// 应用文本区域样式
    /// </summary>
    private void ApplyTextAreaStyles(TextBox textBox)
    {
        // 设置文本颜色
        if (!string.IsNullOrEmpty(TextColor))
        {
            textBox.Foreground = LayoutHelper.ParseColorBrush(TextColor);
        }

        // 设置背景颜色
        if (!string.IsNullOrEmpty(BackgroundColorOverride))
        {
            textBox.Background = LayoutHelper.ParseColorBrush(BackgroundColorOverride);
        }

        // 设置边框
        if (!string.IsNullOrEmpty(BorderColor) && BorderWidth > 0)
        {
            textBox.BorderBrush = LayoutHelper.ParseColorBrush(BorderColor);
            textBox.BorderThickness = new Avalonia.Thickness(BorderWidth);
        }

        // 设置圆角
        if (CornerRadius > 0)
        {
            textBox.CornerRadius = new Avalonia.CornerRadius(CornerRadius);
        }

        // 设置最小高度（基于行数）
        if (MinLines > 0)
        {
            textBox.MinHeight = MinLines * FontSize * 1.5; // 估算行高
        }

        // 设置最大高度（基于行数）
        if (MaxLines > 0)
        {
            textBox.MaxHeight = MaxLines * FontSize * 1.5;
        }
    }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    private void RegisterEvents(TextBox textBox)
    {
        // 文本变化事件
        if (OnChanged != null)
        {
            textBox.TextChanged += (sender, e) =>
            {
                try
                {
                    Value = textBox.Text ?? string.Empty;
                    OnChanged?.Invoke(Value);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[TextArea] Error in change handler: {ex.Message}");
                }
            };
        }

        // 焦点获得事件
        if (OnFocus != null)
        {
            textBox.GotFocus += (sender, e) =>
            {
                try
                {
                    OnFocus?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[TextArea] Error in focus handler: {ex.Message}");
                }
            };
        }

        // 焦点失去事件
        if (OnBlur != null)
        {
            textBox.LostFocus += (sender, e) =>
            {
                try
                {
                    OnBlur?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[TextArea] Error in blur handler: {ex.Message}");
                }
            };
        }
    }

    /// <summary>
    /// 解析字体粗细
    /// </summary>
    private FontWeight ParseFontWeight(string fontWeight)
    {
        return fontWeight.ToLowerInvariant() switch
        {
            "bold" => Avalonia.Media.FontWeight.Bold,
            "bolder" => Avalonia.Media.FontWeight.Bold,
            "lighter" => Avalonia.Media.FontWeight.Light,
            "normal" => Avalonia.Media.FontWeight.Normal,
            "100" => Avalonia.Media.FontWeight.Thin,
            "200" => Avalonia.Media.FontWeight.ExtraLight,
            "300" => Avalonia.Media.FontWeight.Light,
            "400" => Avalonia.Media.FontWeight.Normal,
            "500" => Avalonia.Media.FontWeight.Medium,
            "600" => Avalonia.Media.FontWeight.SemiBold,
            "700" => Avalonia.Media.FontWeight.Bold,
            "800" => Avalonia.Media.FontWeight.ExtraBold,
            "900" => Avalonia.Media.FontWeight.Black,
            _ => Avalonia.Media.FontWeight.Normal
        };
    }

    /// <summary>
    /// 链式调用：设置文本内容
    /// </summary>
    public TextArea SetValue(string value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 链式调用：设置占位符
    /// </summary>
    public TextArea SetPlaceholder(string placeholder)
    {
        Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本变化回调
    /// </summary>
    public TextArea SetOnChanged(Action<string> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置焦点获得回调
    /// </summary>
    public TextArea SetOnFocus(Action onFocus)
    {
        OnFocus = onFocus;
        return this;
    }

    /// <summary>
    /// 链式调用：设置焦点失去回调
    /// </summary>
    public TextArea SetOnBlur(Action onBlur)
    {
        OnBlur = onBlur;
        return this;
    }

    /// <summary>
    /// 链式调用：设置只读状态
    /// </summary>
    public TextArea SetReadOnly(bool readOnly)
    {
        IsReadOnly = readOnly;
        return this;
    }

    /// <summary>
    /// 链式调用：设置禁用状态
    /// </summary>
    public TextArea SetDisabled(bool disabled)
    {
        IsDisabled = disabled;
        return this;
    }

    /// <summary>
    /// 链式调用：设置自动换行
    /// </summary>
    public TextArea SetAutoWrap(bool autoWrap)
    {
        AutoWrap = autoWrap;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public TextArea SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体粗细
    /// </summary>
    public TextArea SetFontWeight(string fontWeight)
    {
        FontWeight = fontWeight;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本颜色
    /// </summary>
    public TextArea SetTextColor(string color)
    {
        TextColor = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置背景颜色
    /// </summary>
    public TextArea SetBackgroundColorOverride(string color)
    {
        BackgroundColorOverride = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置边框
    /// </summary>
    public TextArea SetBorder(string color, double width = 1)
    {
        BorderColor = color;
        BorderWidth = width;
        return this;
    }

    /// <summary>
    /// 链式调用：设置圆角
    /// </summary>
    public TextArea SetCornerRadius(double radius)
    {
        CornerRadius = radius;
        return this;
    }

    /// <summary>
    /// 链式调用：设置内边距
    /// </summary>
    public TextArea SetTextPadding(double left, double top, double right, double bottom)
    {
        TextPadding = new Thickness(left, top, right, bottom);
        return this;
    }

    /// <summary>
    /// 链式调用：设置行数范围
    /// </summary>
    public TextArea SetLinesRange(int minLines, int maxLines)
    {
        MinLines = minLines;
        MaxLines = maxLines;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本对齐
    /// </summary>
    public TextArea SetTextAlignment(Avalonia.Media.TextAlignment alignment)
    {
        TextAlignment = alignment;
        return this;
    }

    /// <summary>
    /// 链式调用：设置最大长度
    /// </summary>
    public TextArea SetMaxLength(int maxLength)
    {
        MaxLength = maxLength;
        return this;
    }
}