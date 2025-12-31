using System.Globalization;

namespace Old8Lang.FirstUI.Theme;

/// <summary>
/// 颜色方案
/// 定义应用的颜色主题
/// </summary>
public class ColorScheme
{
    // 主要颜色
    public string Primary { get; set; } = "#007AFF";
    public string PrimaryVariant { get; set; } = "#0051D5";
    public string OnPrimary { get; set; } = "#FFFFFF";

    // 次要颜色
    public string Secondary { get; set; } = "#5856D6";
    public string SecondaryVariant { get; set; } = "#3634A3";
    public string OnSecondary { get; set; } = "#FFFFFF";

    // 背景颜色
    public string Background { get; set; } = "#FFFFFF";
    public string OnBackground { get; set; } = "#000000";

    // 表面颜色
    public string Surface { get; set; } = "#F2F2F7";
    public string OnSurface { get; set; } = "#000000";

    // 错误颜色
    public string Error { get; set; } = "#FF3B30";
    public string OnError { get; set; } = "#FFFFFF";

    // 成功、警告、信息颜色
    public string Success { get; set; } = "#34C759";
    public string Warning { get; set; } = "#FF9500";
    public string Info { get; set; } = "#5AC8FA";

    // 边框和分隔线
    public string Border { get; set; } = "#C6C6C8";
    public string Divider { get; set; } = "#E5E5EA";

    // 禁用状态
    public string Disabled { get; set; } = "#8E8E93";

    // 文本颜色
    public string TextPrimary { get; set; } = "#000000";
    public string TextSecondary { get; set; } = "#8E8E93";
    public string TextDisabled { get; set; } = "#C7C7CC";

    /// <summary>
    /// 调整颜色的不透明度
    /// </summary>
    /// <param name="color">原始颜色（十六进制格式）</param>
    /// <param name="opacity">不透明度（0.0 - 1.0）</param>
    /// <returns>RGBA 格式的颜色字符串</returns>
    public static string WithOpacity(string color, double opacity)
    {
        if (string.IsNullOrWhiteSpace(color))
            return color;

        // 移除 # 前缀
        color = color.TrimStart('#');

        // 解析 RGB 值
        if (color.Length == 6)
        {
            int r = int.Parse(color.Substring(0, 2), NumberStyles.HexNumber);
            int g = int.Parse(color.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(color.Substring(4, 2), NumberStyles.HexNumber);
            return $"rgba({r}, {g}, {b}, {opacity:F2})";
        }

        return color;
    }

    /// <summary>
    /// 创建浅色主题颜色方案
    /// </summary>
    public static ColorScheme Light() => new()
    {
        Primary = "#007AFF",
        PrimaryVariant = "#0051D5",
        OnPrimary = "#FFFFFF",

        Secondary = "#5856D6",
        SecondaryVariant = "#3634A3",
        OnSecondary = "#FFFFFF",

        Background = "#FFFFFF",
        OnBackground = "#000000",

        Surface = "#F2F2F7",
        OnSurface = "#000000",

        Error = "#FF3B30",
        OnError = "#FFFFFF",

        Success = "#34C759",
        Warning = "#FF9500",
        Info = "#5AC8FA",

        Border = "#C6C6C8",
        Divider = "#E5E5EA",

        Disabled = "#8E8E93",

        TextPrimary = "#000000",
        TextSecondary = "#8E8E93",
        TextDisabled = "#C7C7CC"
    };

    /// <summary>
    /// 创建深色主题颜色方案
    /// </summary>
    public static ColorScheme Dark() => new()
    {
        Primary = "#0A84FF",
        PrimaryVariant = "#409CFF",
        OnPrimary = "#FFFFFF",

        Secondary = "#5E5CE6",
        SecondaryVariant = "#7D7AFF",
        OnSecondary = "#FFFFFF",

        Background = "#000000",
        OnBackground = "#FFFFFF",

        Surface = "#1C1C1E",
        OnSurface = "#FFFFFF",

        Error = "#FF453A",
        OnError = "#FFFFFF",

        Success = "#30D158",
        Warning = "#FF9F0A",
        Info = "#64D2FF",

        Border = "#38383A",
        Divider = "#2C2C2E",

        Disabled = "#636366",

        TextPrimary = "#FFFFFF",
        TextSecondary = "#98989D",
        TextDisabled = "#48484A"
    };

    /// <summary>
    /// 创建 Material Design 颜色方案
    /// </summary>
    public static ColorScheme Material() => new()
    {
        Primary = "#6200EE",
        PrimaryVariant = "#3700B3",
        OnPrimary = "#FFFFFF",

        Secondary = "#03DAC6",
        SecondaryVariant = "#018786",
        OnSecondary = "#000000",

        Background = "#FFFFFF",
        OnBackground = "#000000",

        Surface = "#FFFFFF",
        OnSurface = "#000000",

        Error = "#B00020",
        OnError = "#FFFFFF",

        Success = "#4CAF50",
        Warning = "#FF9800",
        Info = "#2196F3",

        Border = "#E0E0E0",
        Divider = "#BDBDBD",

        Disabled = "#9E9E9E",

        TextPrimary = "#212121",
        TextSecondary = "#757575",
        TextDisabled = "#BDBDBD"
    };

    /// <summary>
    /// 创建 Material Design 深色颜色方案
    /// </summary>
    public static ColorScheme MaterialDark() => new()
    {
        Primary = "#BB86FC",
        PrimaryVariant = "#3700B3",
        OnPrimary = "#000000",

        Secondary = "#03DAC6",
        SecondaryVariant = "#03DAC6",
        OnSecondary = "#000000",

        Background = "#121212",
        OnBackground = "#FFFFFF",

        Surface = "#121212",
        OnSurface = "#FFFFFF",

        Error = "#CF6679",
        OnError = "#000000",

        Success = "#66BB6A",
        Warning = "#FFB74D",
        Info = "#42A5F5",

        Border = "#2C2C2C",
        Divider = "#1F1F1F",

        Disabled = "#5F5F5F",

        TextPrimary = "#FFFFFF",
        TextSecondary = "#B3B3B3",
        TextDisabled = "#5F5F5F"
    };
}
