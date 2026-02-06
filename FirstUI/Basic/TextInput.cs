using Avalonia.Controls;
using FirstUI.Core;
using FirstUI.Utils;

namespace FirstUI.Basic;

/// <summary>
/// TextInput 文本输入框组件
/// </summary>
public class TextInput(string placeholder = "", string initialValue = "") : WidgetBase
{
    /// <summary>
    /// 占位符文本
    /// </summary>
    public string Placeholder { get; set; } = placeholder;

    /// <summary>
    /// 输入值
    /// </summary>
    public string Value { get; set; } = initialValue;

    /// <summary>
    /// 值变化时的回调
    /// </summary>
    public Action<string>? OnChanged { get; set; }

    /// <summary>
    /// 是否为密码模式
    /// </summary>
    public bool IsPassword { get; set; }

    /// <summary>
    /// 是否为多行模式
    /// </summary>
    public bool IsMultiline { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;

    public override object Build(BuildContext context)
    {
        Control inputControl;

        if (IsMultiline)
        {
            // 多行文本框
            var textBox = new TextBox
            {
                Watermark = Placeholder,
                Text = Value,
                AcceptsReturn = true,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                IsReadOnly = IsReadOnly,
                FontSize = FontSize
            };

            if (MaxLength.HasValue)
                textBox.MaxLength = MaxLength.Value;

            // 注册文本变化事件
            textBox.TextChanged += (sender, e) =>
            {
                if (sender is TextBox tb)
                {
                    Value = tb.Text ?? "";
                    OnChanged?.Invoke(Value);
                }
            };

            inputControl = textBox;
        }
        else if (IsPassword)
        {
            // 密码输入框
            var passwordBox = new TextBox
            {
                Watermark = Placeholder,
                Text = Value,
                PasswordChar = '●',
                IsReadOnly = IsReadOnly,
                FontSize = FontSize
            };

            if (MaxLength.HasValue)
                passwordBox.MaxLength = MaxLength.Value;

            // 注册文本变化事件
            passwordBox.TextChanged += (sender, e) =>
            {
                if (sender is TextBox pb)
                {
                    Value = pb.Text ?? "";
                    OnChanged?.Invoke(Value);
                }
            };

            inputControl = passwordBox;
        }
        else
        {
            // 单行文本框
            var textBox = new TextBox
            {
                Watermark = Placeholder,
                Text = Value,
                IsReadOnly = IsReadOnly,
                FontSize = FontSize
            };

            if (MaxLength.HasValue)
                textBox.MaxLength = MaxLength.Value;

            // 注册文本变化事件
            textBox.TextChanged += (sender, e) =>
            {
                if (sender is TextBox tb)
                {
                    Value = tb.Text ?? "";
                    OnChanged?.Invoke(Value);
                }
            };

            inputControl = textBox;
        }

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(inputControl, this);

        // 设置默认内边距（如果未设置）
        if (Padding is { Left: 0, Top: 0, Right: 0, Bottom: 0 })
        {
            if (inputControl is TextBox tb)
            {
                tb.Padding = new Avalonia.Thickness(8, 6);
            }
        }

        return inputControl;
    }

    /// <summary>
    /// 链式调用：设置占位符
    /// </summary>
    public TextInput SetPlaceholder(string placeholder)
    {
        Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// 链式调用：设置值
    /// </summary>
    public TextInput SetValue(string value)
    {
        Value = value;
        return this;
    }

    /// <summary>
    /// 链式调用：设置变化回调
    /// </summary>
    public TextInput SetOnChanged(Action<string> onChanged)
    {
        OnChanged = onChanged;
        return this;
    }

    /// <summary>
    /// 链式调用：设置为密码模式
    /// </summary>
    public TextInput SetPassword(bool isPassword)
    {
        IsPassword = isPassword;
        return this;
    }

    /// <summary>
    /// 链式调用：设置为多行模式
    /// </summary>
    public TextInput SetMultiline(bool isMultiline)
    {
        IsMultiline = isMultiline;
        return this;
    }

    /// <summary>
    /// 链式调用：设置最大长度
    /// </summary>
    public TextInput SetMaxLength(int maxLength)
    {
        MaxLength = maxLength;
        return this;
    }

    /// <summary>
    /// 链式调用：设置只读状态
    /// </summary>
    public TextInput SetReadOnly(bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public TextInput SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }
}