namespace FirstUI.Theme;

/// <summary>
/// 主题数据
/// 包含完整的主题配置
/// </summary>
public class ThemeData
{
    /// <summary>
    /// 主题名称
    /// </summary>
    public string Name { get; set; } = "light";

    /// <summary>
    /// 是否为深色主题
    /// </summary>
    public bool IsDark { get; set; } = false;

    /// <summary>
    /// 颜色方案
    /// </summary>
    public ColorScheme Colors { get; set; } = new();

    /// <summary>
    /// 字体方案
    /// </summary>
    public TypographyScheme Typography { get; set; } = new();

    /// <summary>
    /// 间距方案
    /// </summary>
    public SpacingScheme Spacing { get; set; } = new();

    /// <summary>
    /// 阴影方案
    /// </summary>
    public ShadowScheme Shadows { get; set; } = new();

    /// <summary>
    /// 圆角方案
    /// </summary>
    public BorderRadiusScheme BorderRadius { get; set; } = new();
}
