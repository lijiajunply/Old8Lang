using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// 文本组件
/// </summary>
public class Text : WidgetBase
{
    public string Content { get; set; } = string.Empty;
    public double FontSize { get; set; } = 14;
    public string Color { get; set; } = "#000000";

    public Text() { }

    public Text(string content)
    {
        Content = content;
    }

    public override object Build(BuildContext context)
    {
        var textBlock = new TextBlock
        {
            Text = Content,
            FontSize = FontSize,
            Opacity = Opacity,
            IsVisible = IsVisible
        };

        // 设置基本属性
        if (Width.HasValue) textBlock.Width = Width.Value;
        if (Height.HasValue) textBlock.Height = Height.Value;

        // 设置边距
        textBlock.Margin = new Avalonia.Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom);

        // 设置字体颜色
        try
        {
            if (Color.StartsWith("#"))
            {
                textBlock.Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse(Color));
            }
            else
            {
                // 简化处理：只支持十六进制颜色
                textBlock.Foreground = Brushes.Black;
            }
        }
        catch
        {
            textBlock.Foreground = Brushes.Black;
        }

        return textBlock;
    }
}

/// <summary>
/// 按钮组件
/// </summary>
public class Button : WidgetBase
{
    public string Label { get; set; } = string.Empty;
    public Action? OnClick { get; set; }

    public Button() { }

    public Button(string label)
    {
        Label = label;
    }

    public override object Build(BuildContext context)
    {
        var button = new Avalonia.Controls.Button
        {
            Content = Label,
            Opacity = Opacity,
            IsVisible = IsVisible
        };

        // 设置基本属性
        if (Width.HasValue) button.Width = Width.Value;
        if (Height.HasValue) button.Height = Height.Value;

        // 设置边距
        button.Margin = new Avalonia.Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom);

        // 注册点击事件
        if (OnClick != null)
        {
            button.Click += (s, e) => {
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
}