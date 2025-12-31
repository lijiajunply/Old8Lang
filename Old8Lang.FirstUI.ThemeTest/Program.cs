using System;
using Old8Lang.FirstUI.Theme;

Console.WriteLine("=== Old8Lang.FirstUI 主题系统测试 ===\n");

// 测试 1: 主题创建
Console.WriteLine("【测试 1】主题创建");
var lightTheme = Theme.Light();
Console.WriteLine($"✓ 浅色主题: {lightTheme.Name}, IsDark: {lightTheme.IsDark}");

var darkTheme = Theme.Dark();
Console.WriteLine($"✓ 深色主题: {darkTheme.Name}, IsDark: {darkTheme.IsDark}");

var materialTheme = Theme.Material();
Console.WriteLine($"✓ Material 主题: {materialTheme.Name}");

var materialDarkTheme = Theme.MaterialDark();
Console.WriteLine($"✓ Material Dark 主题: {materialDarkTheme.Name}");

var fluentTheme = Theme.Fluent();
Console.WriteLine($"✓ Fluent 主题: {fluentTheme.Name}");

var fluentDarkTheme = Theme.FluentDark();
Console.WriteLine($"✓ Fluent Dark 主题: {fluentDarkTheme.Name}");

var themes = Theme.GetAvailableThemes();
Console.WriteLine($"✓ 可用主题数量: {themes.Length}");
foreach (var themeName in themes)
{
    Console.WriteLine($"  - {themeName}");
}

// 测试 2: 主题管理器
Console.WriteLine("\n【测试 2】主题管理器");
var manager = ThemeManager.Instance;
Console.WriteLine($"✓ 当前主题: {manager.CurrentTheme.Name}");

manager.SetTheme("dark");
Console.WriteLine($"✓ 切换到深色主题: {manager.CurrentTheme.Name}");

manager.SetTheme("material");
Console.WriteLine($"✓ 切换到 Material: {manager.CurrentTheme.Name}");

manager.SetTheme("fluent");
Console.WriteLine($"✓ 切换到 Fluent: {manager.CurrentTheme.Name}");

manager.ToggleTheme();
Console.WriteLine($"✓ ToggleTheme: {manager.CurrentTheme.Name}");

manager.SetLightTheme();
Console.WriteLine($"✓ SetLightTheme: {manager.CurrentTheme.Name}");

manager.SetDarkTheme();
Console.WriteLine($"✓ SetDarkTheme: {manager.CurrentTheme.Name}");

// 测试 3: 颜色方案
Console.WriteLine("\n【测试 3】颜色方案");
var lightColors = ColorScheme.Light();
Console.WriteLine($"✓ 浅色主题颜色:");
Console.WriteLine($"  Primary: {lightColors.Primary}");
Console.WriteLine($"  Background: {lightColors.Background}");
Console.WriteLine($"  TextPrimary: {lightColors.TextPrimary}");

var darkColors = ColorScheme.Dark();
Console.WriteLine($"✓ 深色主题颜色:");
Console.WriteLine($"  Primary: {darkColors.Primary}");
Console.WriteLine($"  Background: {darkColors.Background}");
Console.WriteLine($"  TextPrimary: {darkColors.TextPrimary}");

var materialColors = ColorScheme.Material();
Console.WriteLine($"✓ Material 主题颜色:");
Console.WriteLine($"  Primary: {materialColors.Primary}");

var colorWithOpacity = ColorScheme.WithOpacity("#FF5722", 0.5);
Console.WriteLine($"✓ 颜色透明度: {colorWithOpacity}");

// 测试 4: 样式表
Console.WriteLine("\n【测试 4】样式表");
var theme = Theme.Light();
var styleSheet = new StyleSheet(theme);

var buttonStyle = styleSheet.Get("button.primary");
Console.WriteLine($"✓ button.primary 样式存在: {buttonStyle != null}");
if (buttonStyle != null)
{
    var bgColor = buttonStyle.Get<string>("backgroundColor");
    var textColor = buttonStyle.Get<string>("textColor");
    Console.WriteLine($"  backgroundColor: {bgColor}");
    Console.WriteLine($"  textColor: {textColor}");
}

var textH1Style = styleSheet.Get("text.h1");
Console.WriteLine($"✓ text.h1 样式存在: {textH1Style != null}");
if (textH1Style != null)
{
    var fontSize = textH1Style.Get<double>("fontSize");
    Console.WriteLine($"  fontSize: {fontSize}");
}

// 注册自定义样式
var customStyle = StyleSheet.Create()
    .Set("backgroundColor", "#FF6B6B")
    .Set("textColor", "#FFFFFF")
    .Set("fontSize", 18)
    .Build();

styleSheet.Register("custom.button", customStyle);
var retrievedStyle = styleSheet.Get("custom.button");
Console.WriteLine($"✓ 自定义样式注册成功: {retrievedStyle != null}");
if (retrievedStyle != null)
{
    var bgColor = retrievedStyle.Get<string>("backgroundColor");
    Console.WriteLine($"  backgroundColor: {bgColor}");
}

// 测试 5: 主题监听
Console.WriteLine("\n【测试 5】主题变化监听");
int callbackCount = 0;
manager.OnThemeChanged(t => {
    callbackCount++;
    Console.WriteLine($"  → 主题变化 #{callbackCount}: {t.Name}");
});

manager.SetTheme("dark");
manager.SetTheme("material");
manager.SetTheme("light");

Console.WriteLine($"✓ 回调被调用次数: {callbackCount}");

manager.ClearListeners();
Console.WriteLine("✓ 清除监听器");

// 测试 6: 字体和间距方案
Console.WriteLine("\n【测试 6】字体和间距方案");
var typography = lightTheme.Typography;
Console.WriteLine($"✓ H1 字体大小: {typography.H1.Size}");
Console.WriteLine($"✓ H1 字体粗细: {typography.H1.Weight}");
Console.WriteLine($"✓ Body1 字体大小: {typography.Body1.Size}");

var spacing = lightTheme.Spacing;
Console.WriteLine($"✓ Small 间距: {spacing.Small}");
Console.WriteLine($"✓ Medium 间距: {spacing.Medium}");
Console.WriteLine($"✓ Large 间距: {spacing.Large}");

// 测试 7: 阴影和圆角方案
Console.WriteLine("\n【测试 7】阴影和圆角方案");
var shadows = lightTheme.Shadows;
Console.WriteLine($"✓ Medium 阴影 Y 偏移: {shadows.Medium.OffsetY}");
Console.WriteLine($"✓ Medium 阴影模糊半径: {shadows.Medium.BlurRadius}");

var borderRadius = lightTheme.BorderRadius;
Console.WriteLine($"✓ Small 圆角: {borderRadius.Small}");
Console.WriteLine($"✓ Medium 圆角: {borderRadius.Medium}");
Console.WriteLine($"✓ Large 圆角: {borderRadius.Large}");

Console.WriteLine("\n=== 所有测试完成 ✓ ===");
