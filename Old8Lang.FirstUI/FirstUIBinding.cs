using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Basic;
using Old8Lang.FirstUI.Layout;
using Old8Lang.FirstUI.Utils;

namespace Old8Lang.FirstUI;

/// <summary>
/// Old8Lang FirstUI 绑定类
/// 提供给 Old8Lang 调用的公共 API
/// </summary>
public static class FirstUIBinding
{
    private static Application? _app;
    private static Window? _mainWindow;
    private static BuildContext? _context;

    /// <summary>
    /// 初始化 FirstUI 库
    /// </summary>
    public static void Initialize()
    {
        if (_app == null)
        {
            _app = new Application();
            _context = new BuildContext();
            Console.WriteLine("[FirstUI] Initialized");
        }
    }

    /// <summary>
    /// 创建应用程序实例
    /// </summary>
    public static object CreateApp()
    {
        Initialize();
        return new FirstUIApplication();
    }

    /// <summary>
    /// 创建组件（从 Old8Lang 字典配置）
    /// </summary>
    public static object? CreateWidget(string widgetType, object? config = null)
    {
        var configDict = TypeConverter.ToDictionary(config);

        return widgetType.ToLower() switch
        {
            // 布局组件
            "container" => CreateContainer(configDict),
            "row" => CreateRow(configDict),
            "column" => CreateColumn(configDict),
            "vstack" => CreateColumn(configDict),
            "hstack" => CreateRow(configDict),
            "stack" => CreateStack(configDict),
            "zstack" => CreateStack(configDict),

            // 基础组件
            "text" => CreateText(configDict),
            "button" => CreateButton(configDict),
            "image" => CreateImage(configDict),

            // 输入组件
            "textinput" => CreateTextInput(configDict),

            _ => null
        };
    }

    #region 组件工厂方法

    private static Container CreateContainer(Dictionary<string, object>? config)
    {
        var container = new Container();
        ApplyCommonProperties(container, config);

        if (config != null)
        {
            if (config.TryGetValue("child", out var child) && child is WidgetBase childWidget)
                container.Child = childWidget;

            container.BorderRadius = TypeConverter.GetDouble(config, "borderRadius", 0);
            container.BorderColor = TypeConverter.GetString(config, "borderColor");
            container.BorderWidth = TypeConverter.GetDouble(config, "borderWidth", 0);
        }

        return container;
    }

    private static Row CreateRow(Dictionary<string, object>? config)
    {
        var row = new Row();
        ApplyCommonProperties(row, config);

        if (config != null)
        {
            if (config.TryGetValue("children", out var children))
            {
                row.Children = TypeConverter.ToList<WidgetBase>(children);
            }

            row.Spacing = TypeConverter.GetDouble(config, "spacing", 0);
        }

        return row;
    }

    private static Column CreateColumn(Dictionary<string, object>? config)
    {
        var column = new Column();
        ApplyCommonProperties(column, config);

        if (config != null)
        {
            if (config.TryGetValue("children", out var children))
            {
                column.Children = TypeConverter.ToList<WidgetBase>(children);
            }

            column.Spacing = TypeConverter.GetDouble(config, "spacing", 0);
        }

        return column;
    }

    private static Stack CreateStack(Dictionary<string, object>? config)
    {
        var stack = new Stack();
        ApplyCommonProperties(stack, config);

        if (config != null)
        {
            if (config.TryGetValue("children", out var children))
            {
                stack.Children = TypeConverter.ToList<WidgetBase>(children);
            }
        }

        return stack;
    }

    private static Text CreateText(Dictionary<string, object>? config)
    {
        var content = TypeConverter.GetString(config, "content", "");
        var text = new Text(content);
        ApplyCommonProperties(text, config);

        if (config != null)
        {
            text.FontSize = TypeConverter.GetDouble(config, "fontSize", 14);
            text.FontWeight = TypeConverter.GetString(config, "fontWeight", "normal");
            text.Color = TypeConverter.GetString(config, "color");
        }

        return text;
    }

    private static Basic.Button CreateButton(Dictionary<string, object>? config)
    {
        var label = TypeConverter.GetString(config, "label", "Button");
        var button = new Basic.Button(label);
        ApplyCommonProperties(button, config);

        if (config != null)
        {
            if (config.TryGetValue("onClick", out var onClick))
            {
                button.OnClick = TypeConverter.WrapAction(onClick);
            }

            button.FontSize = TypeConverter.GetDouble(config, "fontSize", 14);
        }

        return button;
    }

    private static Basic.Image CreateImage(Dictionary<string, object>? config)
    {
        var source = TypeConverter.GetString(config, "source", "");
        var image = new Basic.Image(source);
        ApplyCommonProperties(image, config);

        return image;
    }

    private static TextInput CreateTextInput(Dictionary<string, object>? config)
    {
        var placeholder = TypeConverter.GetString(config, "placeholder", "");
        var textInput = new TextInput(placeholder);
        ApplyCommonProperties(textInput, config);

        if (config != null)
        {
            if (config.TryGetValue("onChanged", out var onChanged))
            {
                textInput.OnChanged = TypeConverter.WrapAction<string>(onChanged);
            }

            textInput.IsPassword = TypeConverter.GetBool(config, "isPassword", false);
            textInput.IsMultiline = TypeConverter.GetBool(config, "isMultiline", false);
        }

        return textInput;
    }

    /// <summary>
    /// 应用通用属性
    /// </summary>
    private static void ApplyCommonProperties(WidgetBase widget, Dictionary<string, object>? config)
    {
        if (config == null) return;

        if (config.TryGetValue("width", out var width))
            widget.Width = Convert.ToDouble(width);

        if (config.TryGetValue("height", out var height))
            widget.Height = Convert.ToDouble(height);

        widget.BackgroundColor = TypeConverter.GetString(config, "backgroundColor");

        if (config.TryGetValue("padding", out var padding))
        {
            if (padding is double paddingValue)
                widget.Padding = new Core.Thickness(paddingValue);
        }

        if (config.TryGetValue("margin", out var margin))
        {
            if (margin is double marginValue)
                widget.Margin = new Core.Thickness(marginValue);
        }
    }

    #endregion

    /// <summary>
    /// 显示 Toast 消息
    /// </summary>
    public static void ShowToast(string message, int duration = 3000)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine($"[FirstUI Toast] {message}");
            // TODO: 实现真正的 Toast UI
        });
    }

    /// <summary>
    /// 切换主题
    /// </summary>
    public static void SetTheme(string themeName)
    {
        Console.WriteLine($"[FirstUI] Switching to theme: {themeName}");
        // TODO: 实现主题切换逻辑
    }

    /// <summary>
    /// 运行应用程序
    /// </summary>
    public static void RunApp(object buildFunction)
    {
        if (_app == null)
        {
            Initialize();
        }

        _mainWindow = new Window
        {
            Title = "Old8Lang FirstUI Application",
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        try
        {
            var invokeMethod = buildFunction.GetType().GetMethod("Invoke");
            var rootWidget = invokeMethod?.Invoke(buildFunction, null);

            if (rootWidget is WidgetBase widget && _context != null)
            {
                if (widget.Build(_context) is Control control)
                {
                    _mainWindow.Content = control;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FirstUI] Error building UI: {ex.Message}");
            Console.Error.WriteLine($"[FirstUI] Stack trace: {ex.StackTrace}");
            _mainWindow.Content = new TextBlock
            {
                Text = $"Error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
        }

        _mainWindow.Show();
    }
}

/// <summary>
/// FirstUI 应用程序类
/// </summary>
public class FirstUIApplication
{
    /// <summary>
    /// 运行应用程序
    /// </summary>
    public void Run(object buildFunction)
    {
        FirstUIBinding.RunApp(buildFunction);
    }
}