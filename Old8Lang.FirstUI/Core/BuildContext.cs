namespace Old8Lang.FirstUI.Core;

/// <summary>
/// 构建上下文
/// 提供组件构建时的环境信息
/// </summary>
public class BuildContext
{
    /// <summary>
    /// 父组件引用
    /// </summary>
    public WidgetBase? Parent { get; set; }

    /// <summary>
    /// 主题配置
    /// </summary>
    public ThemeData? Theme { get; set; }

    /// <summary>
    /// 状态管理器
    /// </summary>
    public StateManager StateManager { get; }

    /// <summary>
    /// 全局状态存储
    /// </summary>
    private readonly Dictionary<string, object> _globalState;

    public BuildContext()
    {
        StateManager = new StateManager();
        _globalState = new Dictionary<string, object>();
    }

    /// <summary>
    /// 获取全局状态
    /// </summary>
    public T? GetGlobalState<T>(string key)
    {
        if (_globalState.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// 设置全局状态
    /// </summary>
    public void SetGlobalState(string key, object value)
    {
        _globalState[key] = value;
    }

    /// <summary>
    /// 移除全局状态
    /// </summary>
    public void RemoveGlobalState(string key)
    {
        _globalState.Remove(key);
    }
}

/// <summary>
/// 主题数据
/// </summary>
public class ThemeData
{
    public string Name { get; set; } = "light";
    public ColorScheme Colors { get; set; } = new();
    public TypographyScheme Typography { get; set; } = new();
    public SpacingScheme Spacing { get; set; } = new();
}

/// <summary>
/// 颜色方案
/// </summary>
public class ColorScheme
{
    public string Primary { get; set; } = "#007AFF";
    public string Secondary { get; set; } = "#5856D6";
    public string Background { get; set; } = "#FFFFFF";
    public string Surface { get; set; } = "#F2F2F7";
    public string Error { get; set; } = "#FF3B30";
    public string OnPrimary { get; set; } = "#FFFFFF";
    public string OnBackground { get; set; } = "#000000";
    public string OnSurface { get; set; } = "#000000";
}

/// <summary>
/// 字体方案
/// </summary>
public class TypographyScheme
{
    public FontStyle H1 { get; set; } = new() { Size = 32, Weight = "bold" };
    public FontStyle H2 { get; set; } = new() { Size = 28, Weight = "bold" };
    public FontStyle H3 { get; set; } = new() { Size = 24, Weight = "bold" };
    public FontStyle Body { get; set; } = new() { Size = 16, Weight = "normal" };
    public FontStyle Caption { get; set; } = new() { Size = 12, Weight = "normal" };
}

/// <summary>
/// 字体样式
/// </summary>
public class FontStyle
{
    public double Size { get; set; }
    public string Weight { get; set; } = "normal";
    public string Family { get; set; } = "sans-serif";
}

/// <summary>
/// 间距方案
/// </summary>
public class SpacingScheme
{
    public double XSmall { get; set; } = 4;
    public double Small { get; set; } = 8;
    public double Medium { get; set; } = 16;
    public double Large { get; set; } = 24;
    public double XLarge { get; set; } = 32;
}
