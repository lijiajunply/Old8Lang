using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Theme;
using Old8Lang.FirstUI.Gesture;
using Old8Lang.FirstUI.Layout;
using Old8Lang.FirstUI.Basic;
using Old8Lang.FirstUI.Advanced;

namespace Old8Lang.FirstUI;

/// <summary>
/// Old8Lang FirstUI 绑定类
/// 提供给 Old8Lang 调用的公共 API
/// </summary>
public static class FirstUIBinding
{
    private static BuildContext? _context;

    /// <summary>
    /// 确保上下文已初始化
    /// </summary>
    private static void EnsureContextInitialized()
    {
        if (_context == null)
        {
            _context = new BuildContext
            {
                Theme = ThemeManager.Instance.CurrentTheme
            };
        }
    }

    /// <summary>
    /// 创建应用程序实例
    /// </summary>
    public static FirstUIApplication CreateApp()
    {
        EnsureContextInitialized();
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
    public static void OnThemeChanged(object? callback)
    {
        if (callback != null)
        {
            ThemeManager.Instance.OnThemeChanged(theme =>
            {
                try
                {
                    var invokeMethod = callback.GetType().GetMethod("Invoke");
                    invokeMethod?.Invoke(callback, [theme.Name]);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[FirstUI] Error in theme change callback: {ex.Message}");
                }
            });
        }
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
                invokeMethod?.Invoke(callback, [data]);
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
                invokeMethod?.Invoke(callback, [data]);
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
    public static void RunApp(object buildFunction, string? s = null)
    {
        try
        {
            // 使用 AppBuilder 启动应用
            var builder = BuildAvaloniaApp(buildFunction, s);
            builder.StartWithClassicDesktopLifetime([]);
        }
        catch (PlatformNotSupportedException ex)
        {
            throw new InvalidOperationException(
                "无法在当前环境中启动 GUI 应用。FirstUI 需要图形界面支持。\n\n" +
                "可能的原因:\n" +
                "1. 您正在通过 SSH 连接运行此应用（SSH 会话无法显示 GUI）\n" +
                "2. 当前环境没有活动的图形会话\n" +
                "3. macOS 可能阻止了应用访问图形界面\n\n" +
                "解决方案:\n" +
                "- 如果通过 SSH 连接: 请在本地终端（Terminal.app）中直接运行\n" +
                "- 如果在本地运行: 请确保已登录桌面环境\n" +
                "- macOS: 检查系统偏好设置 -> 安全性与隐私 -> 辅助功能\n\n" +
                $"详细错误: {ex.Message}",
                ex);
        }
        catch (Exception ex) when (ex.Message.Contains("not supported on this platform"))
        {
            throw new InvalidOperationException(
                "无法在当前环境中启动 GUI 应用。FirstUI 需要图形界面支持。\n\n" +
                "可能的原因:\n" +
                "1. 您正在通过 SSH 连接运行此应用（SSH 会话无法显示 GUI）\n" +
                "2. 当前环境没有活动的图形会话\n" +
                "3. macOS 可能阻止了应用访问图形界面\n\n" +
                "解决方案:\n" +
                "- 如果通过 SSH 连接: 请在本地终端（Terminal.app）中直接运行\n" +
                "- 如果在本地运行: 请确保已登录桌面环境\n" +
                "- macOS: 检查系统偏好设置 -> 安全性与隐私 -> 辅助功能\n\n" +
                $"详细错误: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// 构建 Avalonia 应用
    /// </summary>
    private static AppBuilder BuildAvaloniaApp(object buildFunction, string? s = null)
    {
        return AppBuilder.Configure(() => new FirstUIAvaloniaApp(buildFunction, s))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}

/// <summary>
/// FirstUI Avalonia 应用类
/// </summary>
internal class FirstUIAvaloniaApp(object buildFunction, string? title = null) : Application
{
    private Window? _mainWindow;
    private BuildContext? _context;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 初始化上下文
            _context = new BuildContext
            {
                Theme = ThemeManager.Instance.CurrentTheme
            };

            // 创建主窗口
            _mainWindow = new Window
            {
                Title = title ?? "Old8Lang FirstUI Application",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // 构建 UI
            try
            {
                var invokeMethod = buildFunction.GetType().GetMethod("Invoke");
                var rootWidget = invokeMethod?.Invoke(buildFunction, null);

                if (rootWidget is WidgetBase widget)
                {
                    var control = widget.Build(_context);

                    if (control is Control c)
                    {
                        _mainWindow.Content = c;
                    }
                }
            }
            catch (Exception ex)
            {
                // 创建错误显示窗口
                _mainWindow.Content = new TextBlock
                {
                    Text = $"UI 构建错误:\n{ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(20)
                };
            }

            desktop.MainWindow = _mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
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
    public void Run(object buildFunction, string? s = null)
    {
        FirstUIBinding.RunApp(buildFunction, s);
    }
}