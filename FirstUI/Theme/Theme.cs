namespace FirstUI.Theme;

/// <summary>
/// 主题工厂类
/// 提供创建预定义主题的静态方法
/// </summary>
public static class Theme
{
    /// <summary>
    /// 创建默认浅色主题
    /// </summary>
    public static ThemeData Light() => new()
    {
        Name = "light",
        IsDark = false,
        Colors = ColorScheme.Light(),
        Typography = new TypographyScheme(),
        Spacing = new SpacingScheme(),
        Shadows = new ShadowScheme(),
        BorderRadius = new BorderRadiusScheme()
    };

    /// <summary>
    /// 创建深色主题
    /// </summary>
    public static ThemeData Dark() => new()
    {
        Name = "dark",
        IsDark = true,
        Colors = ColorScheme.Dark(),
        Typography = new TypographyScheme(),
        Spacing = new SpacingScheme(),
        Shadows = new ShadowScheme
        {
            // 深色主题下阴影更明显
            XSmall = new ShadowStyle { OffsetY = 1, BlurRadius = 3, Color = "rgba(0, 0, 0, 0.3)" },
            Small = new ShadowStyle { OffsetY = 2, BlurRadius = 6, Color = "rgba(0, 0, 0, 0.3)" },
            Medium = new ShadowStyle { OffsetY = 4, BlurRadius = 12, Color = "rgba(0, 0, 0, 0.4)" },
            Large = new ShadowStyle { OffsetY = 8, BlurRadius = 20, Color = "rgba(0, 0, 0, 0.4)" },
            XLarge = new ShadowStyle { OffsetY = 12, BlurRadius = 28, Color = "rgba(0, 0, 0, 0.5)" }
        },
        BorderRadius = new BorderRadiusScheme()
    };

    /// <summary>
    /// 创建 Material Design 浅色主题
    /// </summary>
    public static ThemeData Material() => new()
    {
        Name = "material",
        IsDark = false,
        Colors = ColorScheme.Material(),
        Typography = new TypographyScheme
        {
            // Material Design 使用 Roboto 字体
            H1 = new FontStyle { Size = 96, Weight = "300", Family = "Roboto", LineHeight = 1.17, LetterSpacing = -1.5 },
            H2 = new FontStyle { Size = 60, Weight = "300", Family = "Roboto", LineHeight = 1.20, LetterSpacing = -0.5 },
            H3 = new FontStyle { Size = 48, Weight = "normal", Family = "Roboto", LineHeight = 1.17, LetterSpacing = 0 },
            H4 = new FontStyle { Size = 34, Weight = "normal", Family = "Roboto", LineHeight = 1.24, LetterSpacing = 0.25 },
            H5 = new FontStyle { Size = 24, Weight = "normal", Family = "Roboto", LineHeight = 1.33, LetterSpacing = 0 },
            H6 = new FontStyle { Size = 20, Weight = "500", Family = "Roboto", LineHeight = 1.40, LetterSpacing = 0.15 },
            Body1 = new FontStyle { Size = 16, Weight = "normal", Family = "Roboto", LineHeight = 1.50, LetterSpacing = 0.5 },
            Body2 = new FontStyle { Size = 14, Weight = "normal", Family = "Roboto", LineHeight = 1.43, LetterSpacing = 0.25 },
            Button = new FontStyle { Size = 14, Weight = "500", Family = "Roboto", LineHeight = 1.71, LetterSpacing = 1.25 },
            Caption = new FontStyle { Size = 12, Weight = "normal", Family = "Roboto", LineHeight = 1.33, LetterSpacing = 0.4 },
            Label = new FontStyle { Size = 10, Weight = "normal", Family = "Roboto", LineHeight = 1.60, LetterSpacing = 1.5 }
        },
        Spacing = new SpacingScheme
        {
            Unit = 8, // Material Design 使用 8dp 基准
            None = 0,
            XXSmall = 2,
            XSmall = 4,
            Small = 8,
            Medium = 16,
            Large = 24,
            XLarge = 32,
            XXLarge = 48
        },
        Shadows = new ShadowScheme(),
        BorderRadius = new BorderRadiusScheme
        {
            Small = 4,
            Medium = 4, // Material Design 默认使用 4dp 圆角
            Large = 8,
            XLarge = 12
        }
    };

    /// <summary>
    /// 创建 Material Design 深色主题
    /// </summary>
    public static ThemeData MaterialDark() => new()
    {
        Name = "material-dark",
        IsDark = true,
        Colors = ColorScheme.MaterialDark(),
        Typography = new TypographyScheme
        {
            // Material Design 使用 Roboto 字体
            H1 = new FontStyle { Size = 96, Weight = "300", Family = "Roboto", LineHeight = 1.17, LetterSpacing = -1.5 },
            H2 = new FontStyle { Size = 60, Weight = "300", Family = "Roboto", LineHeight = 1.20, LetterSpacing = -0.5 },
            H3 = new FontStyle { Size = 48, Weight = "normal", Family = "Roboto", LineHeight = 1.17, LetterSpacing = 0 },
            H4 = new FontStyle { Size = 34, Weight = "normal", Family = "Roboto", LineHeight = 1.24, LetterSpacing = 0.25 },
            H5 = new FontStyle { Size = 24, Weight = "normal", Family = "Roboto", LineHeight = 1.33, LetterSpacing = 0 },
            H6 = new FontStyle { Size = 20, Weight = "500", Family = "Roboto", LineHeight = 1.40, LetterSpacing = 0.15 },
            Body1 = new FontStyle { Size = 16, Weight = "normal", Family = "Roboto", LineHeight = 1.50, LetterSpacing = 0.5 },
            Body2 = new FontStyle { Size = 14, Weight = "normal", Family = "Roboto", LineHeight = 1.43, LetterSpacing = 0.25 },
            Button = new FontStyle { Size = 14, Weight = "500", Family = "Roboto", LineHeight = 1.71, LetterSpacing = 1.25 },
            Caption = new FontStyle { Size = 12, Weight = "normal", Family = "Roboto", LineHeight = 1.33, LetterSpacing = 0.4 },
            Label = new FontStyle { Size = 10, Weight = "normal", Family = "Roboto", LineHeight = 1.60, LetterSpacing = 1.5 }
        },
        Spacing = new SpacingScheme
        {
            Unit = 8,
            None = 0,
            XXSmall = 2,
            XSmall = 4,
            Small = 8,
            Medium = 16,
            Large = 24,
            XLarge = 32,
            XXLarge = 48
        },
        Shadows = new ShadowScheme
        {
            XSmall = new ShadowStyle { OffsetY = 1, BlurRadius = 3, Color = "rgba(0, 0, 0, 0.4)" },
            Small = new ShadowStyle { OffsetY = 2, BlurRadius = 6, Color = "rgba(0, 0, 0, 0.4)" },
            Medium = new ShadowStyle { OffsetY = 4, BlurRadius = 12, Color = "rgba(0, 0, 0, 0.5)" },
            Large = new ShadowStyle { OffsetY = 8, BlurRadius = 20, Color = "rgba(0, 0, 0, 0.5)" },
            XLarge = new ShadowStyle { OffsetY = 12, BlurRadius = 28, Color = "rgba(0, 0, 0, 0.6)" }
        },
        BorderRadius = new BorderRadiusScheme
        {
            Small = 4,
            Medium = 4,
            Large = 8,
            XLarge = 12
        }
    };

    /// <summary>
    /// 创建 Fluent Design 浅色主题（Windows 11 风格）
    /// </summary>
    public static ThemeData Fluent() => new()
    {
        Name = "fluent",
        IsDark = false,
        Colors = new ColorScheme
        {
            Primary = "#0078D4",
            PrimaryVariant = "#005A9E",
            OnPrimary = "#FFFFFF",

            Secondary = "#0078D4",
            SecondaryVariant = "#005A9E",
            OnSecondary = "#FFFFFF",

            Background = "#F3F3F3",
            OnBackground = "#000000",

            Surface = "#FFFFFF",
            OnSurface = "#000000",

            Error = "#D13438",
            OnError = "#FFFFFF",

            Success = "#107C10",
            Warning = "#FFB900",
            Info = "#0078D4",

            Border = "#E1E1E1",
            Divider = "#E1E1E1",

            Disabled = "#C7C7C7",

            TextPrimary = "#000000",
            TextSecondary = "#605E5C",
            TextDisabled = "#A19F9D"
        },
        Typography = new TypographyScheme
        {
            // Fluent Design 使用 Segoe UI 字体
            H1 = new FontStyle { Size = 46, Weight = "600", Family = "Segoe UI", LineHeight = 1.2 },
            H2 = new FontStyle { Size = 32, Weight = "600", Family = "Segoe UI", LineHeight = 1.3 },
            H3 = new FontStyle { Size = 28, Weight = "600", Family = "Segoe UI", LineHeight = 1.3 },
            H4 = new FontStyle { Size = 24, Weight = "600", Family = "Segoe UI", LineHeight = 1.4 },
            H5 = new FontStyle { Size = 20, Weight = "600", Family = "Segoe UI", LineHeight = 1.4 },
            H6 = new FontStyle { Size = 16, Weight = "600", Family = "Segoe UI", LineHeight = 1.5 },
            Body1 = new FontStyle { Size = 14, Weight = "normal", Family = "Segoe UI", LineHeight = 1.5 },
            Body2 = new FontStyle { Size = 12, Weight = "normal", Family = "Segoe UI", LineHeight = 1.5 },
            Button = new FontStyle { Size = 14, Weight = "600", Family = "Segoe UI", LineHeight = 1.5 },
            Caption = new FontStyle { Size = 12, Weight = "normal", Family = "Segoe UI", LineHeight = 1.4 },
            Label = new FontStyle { Size = 10, Weight = "600", Family = "Segoe UI", LineHeight = 1.2 }
        },
        Spacing = new SpacingScheme
        {
            Unit = 4,
            None = 0,
            XXSmall = 2,
            XSmall = 4,
            Small = 8,
            Medium = 12,
            Large = 20,
            XLarge = 32,
            XXLarge = 40
        },
        Shadows = new ShadowScheme
        {
            XSmall = new ShadowStyle { OffsetY = 2, BlurRadius = 4, Color = "rgba(0, 0, 0, 0.08)" },
            Small = new ShadowStyle { OffsetY = 2, BlurRadius = 8, Color = "rgba(0, 0, 0, 0.12)" },
            Medium = new ShadowStyle { OffsetY = 4, BlurRadius = 16, Color = "rgba(0, 0, 0, 0.14)" },
            Large = new ShadowStyle { OffsetY = 8, BlurRadius = 24, Color = "rgba(0, 0, 0, 0.16)" },
            XLarge = new ShadowStyle { OffsetY = 16, BlurRadius = 32, Color = "rgba(0, 0, 0, 0.18)" }
        },
        BorderRadius = new BorderRadiusScheme
        {
            Small = 2,
            Medium = 4,
            Large = 8,
            XLarge = 8
        }
    };

    /// <summary>
    /// 创建 Fluent Design 深色主题
    /// </summary>
    public static ThemeData FluentDark() => new()
    {
        Name = "fluent-dark",
        IsDark = true,
        Colors = new ColorScheme
        {
            Primary = "#0086F0",
            PrimaryVariant = "#0078D4",
            OnPrimary = "#FFFFFF",

            Secondary = "#0086F0",
            SecondaryVariant = "#0078D4",
            OnSecondary = "#FFFFFF",

            Background = "#202020",
            OnBackground = "#FFFFFF",

            Surface = "#2B2B2B",
            OnSurface = "#FFFFFF",

            Error = "#F85149",
            OnError = "#FFFFFF",

            Success = "#3FB950",
            Warning = "#FFA657",
            Info = "#58A6FF",

            Border = "#3B3B3B",
            Divider = "#3B3B3B",

            Disabled = "#5C5C5C",

            TextPrimary = "#FFFFFF",
            TextSecondary = "#B3B3B3",
            TextDisabled = "#6E6E6E"
        },
        Typography = new TypographyScheme
        {
            H1 = new FontStyle { Size = 46, Weight = "600", Family = "Segoe UI", LineHeight = 1.2 },
            H2 = new FontStyle { Size = 32, Weight = "600", Family = "Segoe UI", LineHeight = 1.3 },
            H3 = new FontStyle { Size = 28, Weight = "600", Family = "Segoe UI", LineHeight = 1.3 },
            H4 = new FontStyle { Size = 24, Weight = "600", Family = "Segoe UI", LineHeight = 1.4 },
            H5 = new FontStyle { Size = 20, Weight = "600", Family = "Segoe UI", LineHeight = 1.4 },
            H6 = new FontStyle { Size = 16, Weight = "600", Family = "Segoe UI", LineHeight = 1.5 },
            Body1 = new FontStyle { Size = 14, Weight = "normal", Family = "Segoe UI", LineHeight = 1.5 },
            Body2 = new FontStyle { Size = 12, Weight = "normal", Family = "Segoe UI", LineHeight = 1.5 },
            Button = new FontStyle { Size = 14, Weight = "600", Family = "Segoe UI", LineHeight = 1.5 },
            Caption = new FontStyle { Size = 12, Weight = "normal", Family = "Segoe UI", LineHeight = 1.4 },
            Label = new FontStyle { Size = 10, Weight = "600", Family = "Segoe UI", LineHeight = 1.2 }
        },
        Spacing = new SpacingScheme
        {
            Unit = 4,
            None = 0,
            XXSmall = 2,
            XSmall = 4,
            Small = 8,
            Medium = 12,
            Large = 20,
            XLarge = 32,
            XXLarge = 40
        },
        Shadows = new ShadowScheme
        {
            XSmall = new ShadowStyle { OffsetY = 2, BlurRadius = 4, Color = "rgba(0, 0, 0, 0.3)" },
            Small = new ShadowStyle { OffsetY = 2, BlurRadius = 8, Color = "rgba(0, 0, 0, 0.4)" },
            Medium = new ShadowStyle { OffsetY = 4, BlurRadius = 16, Color = "rgba(0, 0, 0, 0.5)" },
            Large = new ShadowStyle { OffsetY = 8, BlurRadius = 24, Color = "rgba(0, 0, 0, 0.6)" },
            XLarge = new ShadowStyle { OffsetY = 16, BlurRadius = 32, Color = "rgba(0, 0, 0, 0.7)" }
        },
        BorderRadius = new BorderRadiusScheme
        {
            Small = 2,
            Medium = 4,
            Large = 8,
            XLarge = 8
        }
    };

    /// <summary>
    /// 根据主题名称获取主题
    /// </summary>
    /// <param name="name">主题名称（light, dark, material, material-dark, fluent, fluent-dark）</param>
    /// <returns>主题数据，如果名称不存在则返回 Light 主题</returns>
    public static ThemeData FromName(string name)
    {
        return name.ToLower() switch
        {
            "light" => Light(),
            "dark" => Dark(),
            "material" => Material(),
            "material-dark" => MaterialDark(),
            "fluent" => Fluent(),
            "fluent-dark" => FluentDark(),
            _ => Light()
        };
    }

    /// <summary>
    /// 获取所有可用的主题名称
    /// </summary>
    public static string[] GetAvailableThemes() =>
    [
        "light",
        "dark",
        "material",
        "material-dark",
        "fluent",
        "fluent-dark"
    ];
}
