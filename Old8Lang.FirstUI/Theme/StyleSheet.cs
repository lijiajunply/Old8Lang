namespace Old8Lang.FirstUI.Theme;

/// <summary>
/// 样式属性
/// </summary>
public class Style
{
    private readonly Dictionary<string, object> _properties = new();

    /// <summary>
    /// 设置样式属性
    /// </summary>
    public void Set(string key, object value)
    {
        _properties[key] = value;
    }

    /// <summary>
    /// 获取样式属性
    /// </summary>
    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (_properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// 检查是否包含某个属性
    /// </summary>
    public bool Has(string key) => _properties.ContainsKey(key);

    /// <summary>
    /// 合并另一个样式（其他样式的属性会覆盖当前样式）
    /// </summary>
    public Style Merge(Style other)
    {
        var result = new Style();

        // 复制当前样式的所有属性
        foreach (var kvp in _properties)
        {
            result._properties[kvp.Key] = kvp.Value;
        }

        // 用其他样式的属性覆盖
        foreach (var kvp in other._properties)
        {
            result._properties[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <summary>
    /// 获取所有属性键
    /// </summary>
    public IEnumerable<string> Keys => _properties.Keys;

    /// <summary>
    /// 获取所有属性
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties => _properties;
}

/// <summary>
/// 样式表
/// 管理应用中的所有样式定义
/// </summary>
public class StyleSheet
{
    private readonly Dictionary<string, Style> _styles = new();
    private readonly ThemeData _theme;

    public StyleSheet(ThemeData theme)
    {
        _theme = theme;
        InitializeDefaultStyles();
    }

    /// <summary>
    /// 注册样式
    /// </summary>
    public void Register(string name, Style style)
    {
        _styles[name] = style;
    }

    /// <summary>
    /// 获取样式
    /// </summary>
    public Style? Get(string name)
    {
        return _styles.TryGetValue(name, out var style) ? style : null;
    }

    /// <summary>
    /// 检查是否存在样式
    /// </summary>
    public bool Has(string name) => _styles.ContainsKey(name);

    /// <summary>
    /// 获取所有样式名称
    /// </summary>
    public IEnumerable<string> Names => _styles.Keys;

    /// <summary>
    /// 创建样式构建器
    /// </summary>
    public static StyleBuilder Create() => new();

    /// <summary>
    /// 初始化默认样式
    /// </summary>
    private void InitializeDefaultStyles()
    {
        // 默认按钮样式
        Register("button.primary", Create()
            .Set("backgroundColor", _theme.Colors.Primary)
            .Set("textColor", _theme.Colors.OnPrimary)
            .Set("fontSize", _theme.Typography.Button.Size)
            .Set("fontWeight", _theme.Typography.Button.Weight)
            .Set("padding", _theme.Spacing.Small)
            .Set("borderRadius", _theme.BorderRadius.Medium)
            .Build());

        Register("button.secondary", Create()
            .Set("backgroundColor", _theme.Colors.Secondary)
            .Set("textColor", _theme.Colors.OnSecondary)
            .Set("fontSize", _theme.Typography.Button.Size)
            .Set("fontWeight", _theme.Typography.Button.Weight)
            .Set("padding", _theme.Spacing.Small)
            .Set("borderRadius", _theme.BorderRadius.Medium)
            .Build());

        Register("button.outlined", Create()
            .Set("backgroundColor", "transparent")
            .Set("textColor", _theme.Colors.Primary)
            .Set("borderColor", _theme.Colors.Primary)
            .Set("borderWidth", 1)
            .Set("fontSize", _theme.Typography.Button.Size)
            .Set("fontWeight", _theme.Typography.Button.Weight)
            .Set("padding", _theme.Spacing.Small)
            .Set("borderRadius", _theme.BorderRadius.Medium)
            .Build());

        // 文本样式
        Register("text.h1", Create()
            .Set("fontSize", _theme.Typography.H1.Size)
            .Set("fontWeight", _theme.Typography.H1.Weight)
            .Set("color", _theme.Colors.TextPrimary)
            .Build());

        Register("text.h2", Create()
            .Set("fontSize", _theme.Typography.H2.Size)
            .Set("fontWeight", _theme.Typography.H2.Weight)
            .Set("color", _theme.Colors.TextPrimary)
            .Build());

        Register("text.body", Create()
            .Set("fontSize", _theme.Typography.Body1.Size)
            .Set("fontWeight", _theme.Typography.Body1.Weight)
            .Set("color", _theme.Colors.TextPrimary)
            .Build());

        Register("text.caption", Create()
            .Set("fontSize", _theme.Typography.Caption.Size)
            .Set("fontWeight", _theme.Typography.Caption.Weight)
            .Set("color", _theme.Colors.TextSecondary)
            .Build());

        // 卡片样式
        Register("card", Create()
            .Set("backgroundColor", _theme.Colors.Surface)
            .Set("borderRadius", _theme.BorderRadius.Large)
            .Set("padding", _theme.Spacing.Medium)
            .Set("shadow", _theme.Shadows.Small)
            .Build());

        // 输入框样式
        Register("input", Create()
            .Set("backgroundColor", _theme.Colors.Surface)
            .Set("textColor", _theme.Colors.OnSurface)
            .Set("borderColor", _theme.Colors.Border)
            .Set("borderWidth", 1)
            .Set("borderRadius", _theme.BorderRadius.Small)
            .Set("padding", _theme.Spacing.Small)
            .Set("fontSize", _theme.Typography.Body1.Size)
            .Build());
    }
}

/// <summary>
/// 样式构建器
/// </summary>
public class StyleBuilder
{
    private readonly Style _style = new();

    public StyleBuilder Set(string key, object value)
    {
        _style.Set(key, value);
        return this;
    }

    public Style Build() => _style;
}

/// <summary>
/// 样式扩展方法
/// </summary>
public static class StyleExtensions
{
    /// <summary>
    /// 从主题中应用样式到字典
    /// </summary>
    public static Dictionary<string, object> ApplyStyle(this Dictionary<string, object> dict, Style style)
    {
        foreach (var key in style.Keys)
        {
            if (!dict.ContainsKey(key))
            {
                var value = style.Properties[key];
                dict[key] = value;
            }
        }
        return dict;
    }

    /// <summary>
    /// 从样式表中获取样式并应用到字典
    /// </summary>
    public static Dictionary<string, object> ApplyStyleFrom(
        this Dictionary<string, object> dict,
        StyleSheet styleSheet,
        string styleName)
    {
        var style = styleSheet.Get(styleName);
        if (style != null)
        {
            dict.ApplyStyle(style);
        }
        return dict;
    }
}
