using Avalonia.Controls;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// Text 文本显示组件
/// </summary>
public class Text(string content) : WidgetBase
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Content { get; set; } = content;

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
    public string? Color { get; set; }

    /// <summary>
    /// 文本对齐方式
    /// </summary>
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// 行高
    /// </summary>
    public double? LineHeight { get; set; }

    /// <summary>
    /// 是否自动换行
    /// </summary>
    public bool TextWrapping { get; set; } = true;

    /// <summary>
    /// 字体系列
    /// </summary>
    public string? FontFamily { get; set; }

    public override object Build(BuildContext context)
    {
        var textBlock = new TextBlock
        {
            Text = Content,
            FontSize = FontSize,
            TextWrapping = TextWrapping ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap
        };

        // 应用基础样式
        LayoutHelper.ApplyBaseStyles(textBlock, this);

        // 设置字体粗细
        textBlock.FontWeight = FontWeight.ToLower() switch
        {
            "bold" => Avalonia.Media.FontWeight.Bold,
            "semibold" => Avalonia.Media.FontWeight.SemiBold,
            "light" => Avalonia.Media.FontWeight.Light,
            _ => Avalonia.Media.FontWeight.Normal
        };

        // 设置文本颜色
        if (!string.IsNullOrEmpty(Color))
        {
            textBlock.Foreground = LayoutHelper.ParseColorBrush(Color);
        }

        // 设置文本对齐
        textBlock.TextAlignment = TextAlignment switch
        {
            TextAlignment.Left => Avalonia.Media.TextAlignment.Left,
            TextAlignment.Center => Avalonia.Media.TextAlignment.Center,
            TextAlignment.Right => Avalonia.Media.TextAlignment.Right,
            TextAlignment.Justify => Avalonia.Media.TextAlignment.Justify,
            _ => Avalonia.Media.TextAlignment.Left
        };

        // 设置行高
        if (LineHeight.HasValue)
        {
            textBlock.LineHeight = LineHeight.Value;
        }

        // 设置字体系列
        if (!string.IsNullOrEmpty(FontFamily))
        {
            textBlock.FontFamily = new FontFamily(FontFamily);
        }

        return textBlock;
    }

    /// <summary>
    /// 链式调用：设置字体大小
    /// </summary>
    public Text SetFontSize(double size)
    {
        FontSize = size;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体粗细
    /// </summary>
    public Text SetFontWeight(string weight)
    {
        FontWeight = weight;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本颜色
    /// </summary>
    public Text SetColor(string color)
    {
        Color = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置文本对齐
    /// </summary>
    public Text SetTextAlignment(TextAlignment alignment)
    {
        TextAlignment = alignment;
        return this;
    }

    /// <summary>
    /// 链式调用：设置行高
    /// </summary>
    public Text SetLineHeight(double lineHeight)
    {
        LineHeight = lineHeight;
        return this;
    }

    /// <summary>
    /// 链式调用：设置是否自动换行
    /// </summary>
    public Text SetTextWrapping(bool wrapping)
    {
        TextWrapping = wrapping;
        return this;
    }

    /// <summary>
    /// 链式调用：设置字体系列
    /// </summary>
    public Text SetFontFamily(string fontFamily)
    {
        FontFamily = fontFamily;
        return this;
    }
}

/// <summary>
/// 文本对齐枚举
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify
}
