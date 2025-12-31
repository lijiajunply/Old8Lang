# 主题系统示例

演示 Old8Lang.FirstUI 的主题系统功能。

## 示例 1：基础主题切换

```old8
import "firstui" as ui

app <- ui.App()

MainView() -> {
    return ui.Column({
        children: {
            ui.Text("主题系统示例", {
                fontSize: 28,
                fontWeight: "bold"
            }),

            ui.Text("当前主题: " + ui.GetCurrentTheme(), {
                fontSize: 16,
                margin: {top: 20}
            }),

            // 切换到浅色主题
            ui.Button("切换到浅色主题", {
                onClick: () -> {
                    ui.SetTheme("light")
                }
            }),

            // 切换到深色主题
            ui.Button("切换到深色主题", {
                onClick: () -> {
                    ui.SetTheme("dark")
                }
            }),

            // 切换到 Material Design
            ui.Button("切换到 Material Design", {
                onClick: () -> {
                    ui.SetTheme("material")
                }
            }),

            // 切换到 Fluent Design
            ui.Button("切换到 Fluent Design", {
                onClick: () -> {
                    ui.SetTheme("fluent")
                }
            }),

            // 快速切换浅色/深色
            ui.Button("快速切换浅色/深色", {
                onClick: () -> {
                    ui.ToggleTheme()
                }
            })
        }
    })
}

app.Run(() -> MainView())
```

## 示例 2：主题变化监听

```old8
import "firstui" as ui

app <- ui.App()

currentThemeName <- "light"

// 注册主题变化监听器
ui.OnThemeChanged((themeName) -> {
    currentThemeName <- themeName
    PrintLine("主题已切换到: " + themeName)
})

MainView() -> {
    return ui.Column({
        children: {
            ui.Text("主题监听示例", {
                fontSize: 24,
                fontWeight: "bold"
            }),

            ui.Text("当前主题: " + currentThemeName, {
                fontSize: 18
            }),

            ui.Button("随机切换主题", {
                onClick: () -> {
                    themes <- ui.GetAvailableThemes()
                    randomIndex <- RandomInt(0, themes.Length)
                    ui.SetTheme(themes[randomIndex])
                }
            })
        }
    })
}

app.Run(() -> MainView())
```

## 示例 3：展示所有可用主题

```old8
import "firstui" as ui

app <- ui.App()

MainView() -> {
    themes <- ui.GetAvailableThemes()
    buttons <- {}

    for theme in themes {
        buttons.Add(ui.Button("主题: " + theme, {
            onClick: () -> {
                ui.SetTheme(theme)
                ui.ShowToast("已切换到 " + theme + " 主题")
            }
        }))
    }

    return ui.Column({
        children: {
            ui.Text("所有可用主题", {
                fontSize: 28,
                fontWeight: "bold"
            }),
            ui.Text("点击按钮切换主题", {
                fontSize: 14,
                color: "#666"
            })
        } + buttons
    })
}

app.Run(() -> MainView())
```

## 可用主题列表

- **light**: 默认浅色主题（iOS 风格）
- **dark**: 深色主题（iOS 风格）
- **material**: Material Design 浅色主题（Android 风格）
- **material-dark**: Material Design 深色主题
- **fluent**: Fluent Design 浅色主题（Windows 11 风格）
- **fluent-dark**: Fluent Design 深色主题

## 主题 API

### FirstUIBinding 静态方法

```csharp
// 设置主题（通过名称）
FirstUIBinding.SetTheme("dark")

// 获取当前主题名称
string themeName = FirstUIBinding.GetCurrentTheme()

// 获取所有可用主题
string[] themes = FirstUIBinding.GetAvailableThemes()

// 快速切换浅色/深色
FirstUIBinding.ToggleTheme()

// 注册主题变化监听器
FirstUIBinding.OnThemeChanged((themeName) => {
    Console.WriteLine($"Theme changed to: {themeName}");
})
```

### ThemeManager API

```csharp
using Old8Lang.FirstUI.Theme;

// 获取单例实例
var manager = ThemeManager.Instance;

// 设置主题
manager.SetTheme(Theme.Dark());
manager.SetTheme("material");

// 切换主题
manager.SetLightTheme();
manager.SetDarkTheme();
manager.ToggleTheme();

// 获取当前主题
ThemeData currentTheme = manager.CurrentTheme;

// 获取样式表
StyleSheet styleSheet = manager.StyleSheet;

// 注册自定义样式
Style customButtonStyle = StyleSheet.Create()
    .Set("backgroundColor", "#FF5722")
    .Set("textColor", "#FFFFFF")
    .Set("borderRadius", 8)
    .Build();
manager.RegisterStyle("button.custom", customButtonStyle);

// 监听主题变化
manager.OnThemeChanged(theme => {
    Console.WriteLine($"Theme changed: {theme.Name}");
});
```

## 主题属性访问

在组件中访问主题属性：

```csharp
public override object Build(BuildContext context)
{
    var theme = context.Theme;

    // 访问颜色
    string primaryColor = theme.Colors.Primary;
    string backgroundColor = theme.Colors.Background;

    // 访问字体
    double h1Size = theme.Typography.H1.Size;
    string h1Weight = theme.Typography.H1.Weight;

    // 访问间距
    double smallSpacing = theme.Spacing.Small;
    double mediumSpacing = theme.Spacing.Medium;

    // 访问阴影
    var shadow = theme.Shadows.Medium;

    // 访问圆角
    double borderRadius = theme.BorderRadius.Large;

    // 根据主题调整行为
    if (theme.IsDark)
    {
        // 深色主题特定逻辑
    }
}
```

## 自定义主题

创建自定义主题：

```csharp
using Old8Lang.FirstUI.Theme;

var customTheme = new ThemeData
{
    Name = "my-custom-theme",
    IsDark = false,
    Colors = new ColorScheme
    {
        Primary = "#FF6B6B",
        Secondary = "#4ECDC4",
        Background = "#F7FFF7",
        // ... 其他颜色
    },
    Typography = new TypographyScheme
    {
        H1 = new FontStyle { Size = 36, Weight = "bold", Family = "Arial" },
        // ... 其他字体
    },
    // ... 其他配置
};

ThemeManager.Instance.SetTheme(customTheme);
```

## 样式表使用

```csharp
// 获取预定义样式
var buttonStyle = ThemeManager.Instance.GetStyle("button.primary");

// 在字典中应用样式
var config = new Dictionary<string, object>
{
    { "text", "Click me" }
};
config.ApplyStyleFrom(ThemeManager.Instance.StyleSheet, "button.primary");

// 注册自定义样式
var myStyle = StyleSheet.Create()
    .Set("backgroundColor", "#FF5722")
    .Set("padding", 16)
    .Set("borderRadius", 8)
    .Build();

ThemeManager.Instance.RegisterStyle("card.highlighted", myStyle);
```

## 注意事项

1. 主题切换是全局操作，会影响整个应用
2. 主题变化监听器在主题切换时会被调用
3. BuildContext 中的 Theme 属性会在主题切换时自动更新
4. 深色主题会自动调整阴影和对比度以获得更好的视觉效果
5. 不同设计系统（Material、Fluent）有不同的字体、间距和圆角规范
