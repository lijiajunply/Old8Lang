namespace FirstUI.Theme;

/// <summary>
/// 阴影样式
/// </summary>
public class ShadowStyle
{
    public double OffsetX { get; set; } = 0;
    public double OffsetY { get; set; } = 2;
    public double BlurRadius { get; set; } = 4;
    public string Color { get; set; } = "rgba(0, 0, 0, 0.1)";
}

/// <summary>
/// 阴影方案
/// 定义应用的阴影系统
/// </summary>
public class ShadowScheme
{
    public ShadowStyle None { get; set; } = new() { BlurRadius = 0, OffsetY = 0 };
    public ShadowStyle XSmall { get; set; } = new() { OffsetY = 1, BlurRadius = 2 };
    public ShadowStyle Small { get; set; } = new() { OffsetY = 2, BlurRadius = 4 };
    public ShadowStyle Medium { get; set; } = new() { OffsetY = 4, BlurRadius = 8 };
    public ShadowStyle Large { get; set; } = new() { OffsetY = 8, BlurRadius = 16 };
    public ShadowStyle XLarge { get; set; } = new() { OffsetY = 12, BlurRadius = 24 };
}

/// <summary>
/// 圆角方案
/// </summary>
public class BorderRadiusScheme
{
    public double None { get; set; } = 0;
    public double Small { get; set; } = 4;
    public double Medium { get; set; } = 8;
    public double Large { get; set; } = 12;
    public double XLarge { get; set; } = 16;
    public double Circle { get; set; } = 9999;
}
