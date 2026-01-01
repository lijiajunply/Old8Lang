using Avalonia.Controls;
using Avalonia.Media;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI.Basic;

/// <summary>
/// Icon 图标组件
/// 支持 Material Icons、Font Awesome、SVG 等多种图标库
/// </summary>
public class Icon : WidgetBase
{
    /// <summary>
    /// 图标名称或 Unicode 字符
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标库类型
    /// </summary>
    public IconType Type { get; set; } = IconType.Material;

    /// <summary>
    /// 图标大小
    /// </summary>
    public double Size { get; set; } = 24;

    /// <summary>
    /// 图标颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 图标路径（用于 SVG 或图片文件）
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Unicode 字符（直接使用字符编码）
    /// </summary>
    public string? Unicode { get; set; }

    public override object Build(BuildContext context)
    {
        Control iconControl;

        switch (Type)
        {
            case IconType.Material:
                iconControl = BuildMaterialIcon();
                break;
            case IconType.FontAwesome:
                iconControl = BuildFontAwesomeIcon();
                break;
            case IconType.Svg:
                iconControl = BuildPlaceholderIcon("SVG");
                break;
            case IconType.Image:
                iconControl = BuildPlaceholderIcon("IMG");
                break;
            case IconType.Unicode:
                iconControl = BuildUnicodeIcon();
                break;
            default:
                iconControl = BuildMaterialIcon();
                break;
        }

        // 应用基础样式
        Utils.LayoutHelper.ApplyBaseStyles(iconControl, this);

        // 设置大小
        iconControl.Width = Size;
        iconControl.Height = Size;

        return iconControl;
    }

    /// <summary>
    /// 构建 Material Design 图标
    /// </summary>
    private Control BuildMaterialIcon()
    {
        var textBlock = new TextBlock();

        // 使用 Unicode 字符或名称
        if (!string.IsNullOrEmpty(Unicode))
        {
            textBlock.Text = Unicode;
        }
        else if (!string.IsNullOrEmpty(Name))
        {
            // 尝试将名称转换为 Unicode
            textBlock.Text = GetMaterialIconUnicode(Name);
        }
        else
        {
            textBlock.Text = ""; // 默认 Material Icons 的错误图标
        }

        // 设置字体（Material Icons）
        textBlock.FontFamily = new FontFamily("Material Icons");
        textBlock.FontSize = Size;
        textBlock.TextAlignment = Avalonia.Media.TextAlignment.Center;
        textBlock.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        textBlock.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;

        // 设置颜色
        if (!string.IsNullOrEmpty(Color))
        {
            textBlock.Foreground = LayoutHelper.ParseColorBrush(Color);
        }

        return textBlock;
    }

    /// <summary>
    /// 构建 Font Awesome 图标
    /// </summary>
    private Control BuildFontAwesomeIcon()
    {
        var textBlock = new TextBlock();

        // 使用 Unicode 字符或名称
        if (!string.IsNullOrEmpty(Unicode))
        {
            textBlock.Text = Unicode;
        }
        else if (!string.IsNullOrEmpty(Name))
        {
            // 尝试将名称转换为 Unicode
            textBlock.Text = GetFontAwesomeIconUnicode(Name);
        }
        else
        {
            textBlock.Text = ""; // 默认 Font Awesome 的错误图标
        }

        // 设置字体（Font Awesome）
        textBlock.FontFamily = new FontFamily("Font Awesome 6 Free");
        textBlock.FontSize = Size;
        textBlock.TextAlignment = Avalonia.Media.TextAlignment.Center;
        textBlock.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        textBlock.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;

        // 设置颜色
        if (!string.IsNullOrEmpty(Color))
        {
            textBlock.Foreground = LayoutHelper.ParseColorBrush(Color);
        }

        return textBlock;
    }

    /// <summary>
    /// 构建占位符图标
    /// </summary>
    private Control BuildPlaceholderIcon(string type)
    {
        return new TextBlock { Text = "🚫", FontSize = Size };
    }

    /// <summary>
    /// 构建 Unicode 图标
    /// </summary>
    private Control BuildUnicodeIcon()
    {
        var textBlock = new TextBlock
        {
            Text = Unicode ?? "❓",
            FontSize = Size,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        // 设置颜色
        if (!string.IsNullOrEmpty(Color))
        {
            textBlock.Foreground = LayoutHelper.ParseColorBrush(Color);
        }

        return textBlock;
    }

    /// <summary>
    /// 获取 Material Icons 的 Unicode 字符
    /// </summary>
    private string GetMaterialIconUnicode(string name)
    {
        // 这里只提供部分常用图标的映射，实际使用时应该更完整
        var materialIcons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 导航
            { "home", "" },
            { "menu", "" },
            { "back", "" },
            { "forward", "" },
            { "arrow_back", "" },
            { "arrow_forward", "" },
            
            // 操作
            { "add", "" },
            { "remove", "" },
            { "edit", "" },
            { "delete", "" },
            { "save", "" },
            { "cancel", "" },
            { "check", "" },
            { "close", "" },
            
            // 通信
            { "mail", "" },
            { "phone", "" },
            { "message", "" },
            
            // 媒体
            { "play", "" },
            { "pause", "" },
            { "stop", "" },
            { "volume_up", "" },
            { "volume_down", "" },
            { "volume_mute", "" },
            
            // 文件
            { "file", "" },
            { "folder", "" },
            { "download", "" },
            { "upload", "" },
            
            // 设置
            { "settings", "" },
            { "search", "" },
            { "filter", "" },
            
            // 状态
            { "warning", "" },
            { "error", "" },
            { "info", "" },
            { "success", "" }
        };

        return materialIcons.TryGetValue(name, out var unicode) ? unicode : "";
    }

    /// <summary>
    /// 获取 Font Awesome 的 Unicode 字符
    /// </summary>
    private string GetFontAwesomeIconUnicode(string name)
    {
        // 这里只提供部分常用图标的映射，实际使用时应该更完整
        var fontAwesomeIcons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 实心图标
            { "home", "" },
            { "user", "" },
            { "cog", "" },
            { "gear", "" },
            { "search", "" },
            { "heart", "" },
            { "star", "" },
            { "plus", "" },
            { "minus", "" },
            { "times", "" },
            { "close", "" },
            { "check", "" },
            { "edit", "" },
            { "trash", "" },
            { "file", "" },
            { "folder", "" },
            { "download", "" },
            { "upload", "" },
            
            // 箭头
            { "arrow_left", "" },
            { "arrow_right", "" },
            { "arrow_up", "" },
            { "arrow_down", "" },
            
            // 媒体
            { "play", "" },
            { "pause", "" },
            { "stop", "" },
            { "volume_up", "" },
            { "volume_down", "" },
            { "volume_mute", "" },
            
            // 通信
            { "phone", "" },
            { "envelope", "" },
            { "mail", "" },
            
            // 状态
            { "warning", "" },
            { "exclamation", "" },
            { "info", "" },
            { "info_circle", "" }
        };

        return fontAwesomeIcons.TryGetValue(name, out var unicode) ? unicode : "";
    }

    /// <summary>
    /// 链式调用：设置图标名称
    /// </summary>
    public Icon SetName(string name)
    {
        Name = name;
        return this;
    }

    /// <summary>
    /// 链式调用：设置图标类型
    /// </summary>
    public Icon SetType(IconType type)
    {
        Type = type;
        return this;
    }

    /// <summary>
    /// 链式调用：设置大小
    /// </summary>
    public Icon SetSize(double size)
    {
        Size = size;
        return this;
    }

    /// <summary>
    /// 链式调用：设置颜色
    /// </summary>
    public Icon SetColor(string color)
    {
        Color = color;
        return this;
    }

    /// <summary>
    /// 链式调用：设置路径
    /// </summary>
    public Icon SetPath(string path)
    {
        Path = path;
        return this;
    }

    /// <summary>
    /// 链式调用：设置 Unicode 字符
    /// </summary>
    public Icon SetUnicode(string unicode)
    {
        Unicode = unicode;
        return this;
    }
}

/// <summary>
/// 图标类型
/// </summary>
public enum IconType
{
    /// <summary>
    /// Material Design Icons
    /// </summary>
    Material,

    /// <summary>
    /// Font Awesome
    /// </summary>
    FontAwesome,

    /// <summary>
    /// SVG 文件
    /// </summary>
    Svg,

    /// <summary>
    /// 图片文件
    /// </summary>
    Image,

    /// <summary>
    /// Unicode 字符
    /// </summary>
    Unicode
}