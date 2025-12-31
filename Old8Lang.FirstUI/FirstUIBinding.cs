namespace Old8Lang.FirstUI;

/// <summary>
/// Old8Lang FirstUI 绑定类
/// 提供给 Old8Lang 语言使用的 GUI 功能入口点
/// </summary>
public static class FirstUIBinding
{

    /// <summary>
    /// 初始化 FirstUI 库
    /// </summary>
    public static void Initialize()
    {
        // TODO: 初始化 Avalonia 应用程序
        Console.WriteLine("[FirstUI] Initializing Old8Lang.FirstUI");
    }

    /// <summary>
    /// 创建应用程序实例
    /// </summary>
    /// <returns>应用程序对象</returns>
    public static object CreateApp()
    {
        // TODO: 实现应用程序创建逻辑
        throw new NotImplementedException("CreateApp is not yet implemented");
    }

    /// <summary>
    /// 创建组件
    /// </summary>
    /// <param name="widgetType">组件类型名称（如 "Text", "Button" 等）</param>
    /// <param name="config">配置字典（Old8Lang 对象）</param>
    /// <returns>组件实例</returns>
    public static object CreateWidget(string widgetType, object? config = null)
    {
        // TODO: 根据类型创建对应的组件
        throw new NotImplementedException($"Widget type '{widgetType}' is not yet implemented");
    }

    /// <summary>
    /// 显示 Toast 消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="duration">显示时长（毫秒）</param>
    public static void ShowToast(string message, int duration = 3000)
    {
        // TODO: 实现 Toast 显示逻辑
        Console.WriteLine($"[FirstUI Toast] {message} (duration: {duration}ms)");
    }

    /// <summary>
    /// 显示对话框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <returns>用户操作结果</returns>
    public static object ShowDialog(string title, string content)
    {
        // TODO: 实现对话框显示逻辑
        throw new NotImplementedException("ShowDialog is not yet implemented");
    }

    /// <summary>
    /// 设置应用主题
    /// </summary>
    /// <param name="themeName">主题名称（"light" 或 "dark"）</param>
    public static void SetTheme(string themeName)
    {
        // TODO: 实现主题切换逻辑
        Console.WriteLine($"[FirstUI] Setting theme to: {themeName}");
    }
}
