namespace FirstUI.Theme;

/// <summary>
/// 间距方案
/// 定义应用的间距系统
/// </summary>
public class SpacingScheme
{
    // 基础间距单位
    public double Unit { get; set; } = 4;

    // 预定义间距值
    public double None { get; set; } = 0;
    public double XXSmall { get; set; } = 2;
    public double XSmall { get; set; } = 4;
    public double Small { get; set; } = 8;
    public double Medium { get; set; } = 16;
    public double Large { get; set; } = 24;
    public double XLarge { get; set; } = 32;
    public double XXLarge { get; set; } = 48;

    /// <summary>
    /// 获取倍数间距
    /// </summary>
    /// <param name="multiplier">倍数</param>
    public double Get(double multiplier) => Unit * multiplier;
}
