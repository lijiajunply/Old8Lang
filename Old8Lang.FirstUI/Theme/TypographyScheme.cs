namespace Old8Lang.FirstUI.Theme;

/// <summary>
/// 字体样式
/// </summary>
public class FontStyle
{
    public double Size { get; set; }
    public string Weight { get; set; } = "normal";
    public string Family { get; set; } = "sans-serif";
    public double LineHeight { get; set; } = 1.5;
    public double LetterSpacing { get; set; } = 0;
}

/// <summary>
/// 字体方案
/// 定义应用的字体排版系统
/// </summary>
public class TypographyScheme
{
    // 标题样式
    public FontStyle H1 { get; set; } = new() { Size = 32, Weight = "bold", LineHeight = 1.2 };
    public FontStyle H2 { get; set; } = new() { Size = 28, Weight = "bold", LineHeight = 1.3 };
    public FontStyle H3 { get; set; } = new() { Size = 24, Weight = "bold", LineHeight = 1.3 };
    public FontStyle H4 { get; set; } = new() { Size = 20, Weight = "bold", LineHeight = 1.4 };
    public FontStyle H5 { get; set; } = new() { Size = 18, Weight = "bold", LineHeight = 1.4 };
    public FontStyle H6 { get; set; } = new() { Size = 16, Weight = "bold", LineHeight = 1.5 };

    // 正文样式
    public FontStyle Body1 { get; set; } = new() { Size = 16, Weight = "normal", LineHeight = 1.5 };
    public FontStyle Body2 { get; set; } = new() { Size = 14, Weight = "normal", LineHeight = 1.5 };

    // 按钮样式
    public FontStyle Button { get; set; } = new() { Size = 14, Weight = "medium", LineHeight = 1.5 };

    // 说明文字
    public FontStyle Caption { get; set; } = new() { Size = 12, Weight = "normal", LineHeight = 1.4 };

    // 标签样式
    public FontStyle Label { get; set; } = new() { Size = 10, Weight = "medium", LineHeight = 1.2 };
}
