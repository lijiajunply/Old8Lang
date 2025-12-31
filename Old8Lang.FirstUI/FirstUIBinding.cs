using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Theme;
using Old8Lang.FirstUI.Gesture;

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
            _context = new BuildContext
            {
                Theme = ThemeManager.Instance.CurrentTheme
            };
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
        Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine($"[FirstUI] Switching to theme: {themeName}");
            ThemeManager.Instance.SetTheme(themeName);

            // 更新上下文中的主题
            if (_context != null)
            {
                _context.Theme = ThemeManager.Instance.CurrentTheme;
            }

            // TODO: 触发 UI 重建
        });
    }

    /// <summary>
    /// 获取当前主题名称
    /// </summary>
    public static string GetCurrentTheme()
    {
        return ThemeManager.Instance.CurrentTheme.Name;
    }

    /// <summary>
    /// 获取所有可用主题
    /// </summary>
    public static string[] GetAvailableThemes()
    {
        return ThemeManager.GetAvailableThemes();
    }

    /// <summary>
    /// 切换浅色/深色主题
    /// </summary>
    public static void ToggleTheme()
    {
        Dispatcher.UIThread.Post(() =>
        {
            ThemeManager.Instance.ToggleTheme();

            // 更新上下文中的主题
            if (_context != null)
            {
                _context.Theme = ThemeManager.Instance.CurrentTheme;
            }

            Console.WriteLine($"[FirstUI] Theme toggled to: {ThemeManager.Instance.CurrentTheme.Name}");
        });
    }

    /// <summary>
    /// 注册主题变化监听器
    /// </summary>
    public static void OnThemeChanged(object callback)
    {
        if (callback != null)
        {
            ThemeManager.Instance.OnThemeChanged(theme =>
            {
                try
                {
                    var invokeMethod = callback.GetType().GetMethod("Invoke");
                    invokeMethod?.Invoke(callback, new object[] { theme.Name });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[FirstUI] Error in theme change callback: {ex.Message}");
                }
            });
        }
    }

    // ======== 手势相关 API ========

    /// <summary>
    /// 创建手势检测器
    /// </summary>
    public static GestureDetector CreateGestureDetector(WidgetBase child)
    {
        return new GestureDetector { Child = child };
    }

    /// <summary>
    /// 创建可拖动组件
    /// </summary>
    public static Draggable CreateDraggable(WidgetBase child)
    {
        return new Draggable { Child = child };
    }

    /// <summary>
    /// 创建拖放目标
    /// </summary>
    public static DropTarget CreateDropTarget(WidgetBase child)
    {
        return new DropTarget { Child = child };
    }

    /// <summary>
    /// 包装 Old8Lang 回调为 GestureEventData 回调
    /// </summary>
    public static Action<GestureEventData> WrapGestureCallback(object callback)
    {
        return (data) =>
        {
            try
            {
                var invokeMethod = callback.GetType().GetMethod("Invoke");
                invokeMethod?.Invoke(callback, new object[] { data });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FirstUI] Error in gesture callback: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// 包装 Old8Lang 回调为 DragDropData 回调
    /// </summary>
    public static Action<DragDropData> WrapDragDropCallback(object callback)
    {
        return (data) =>
        {
            try
            {
                var invokeMethod = callback.GetType().GetMethod("Invoke");
                invokeMethod?.Invoke(callback, new object[] { data });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FirstUI] Error in drag-drop callback: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// 包装 Old8Lang 简单回调
    /// </summary>
    public static Action WrapSimpleCallback(object callback)
    {
        return () =>
        {
            try
            {
                var invokeMethod = callback.GetType().GetMethod("Invoke");
                invokeMethod?.Invoke(callback, null);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FirstUI] Error in callback: {ex.Message}");
            }
        };
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