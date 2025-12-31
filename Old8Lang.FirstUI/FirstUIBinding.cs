using Avalonia;
using Avalonia.Controls;
using Old8Lang.FirstUI.Core;

namespace Old8Lang.FirstUI;

/// <summary>
/// Old8Lang FirstUI 绑定类
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
    /// 显示 Toast 消息
    /// </summary>
    public static void ShowToast(string message, int duration = 3000)
    {
        Console.WriteLine($"[FirstUI Toast] {message}");
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
            // 调用 Old8Lang 函数构建根组件
            var invokeMethod = buildFunction.GetType().GetMethod("Invoke");
            var rootWidget = invokeMethod?.Invoke(buildFunction, null);

            if (rootWidget is WidgetBase widget && _context != null)
            {
                var control = widget.Build(_context) as Control;
                if (control != null)
                {
                    _mainWindow.Content = control;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FirstUI] Error building UI: {ex.Message}");
            _mainWindow.Content = new TextBlock { Text = $"Error: {ex.Message}" };
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