namespace FirstUI.Theme;

/// <summary>
/// 主题管理器
/// 管理应用的主题切换和样式表
/// </summary>
public class ThemeManager
{
    private static ThemeManager? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static ThemeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ThemeManager();
                }
            }
            return _instance;
        }
    }

    private ThemeData _currentTheme;
    private StyleSheet _styleSheet;
    private readonly List<Action<ThemeData>> _themeChangeListeners = [];

    private ThemeManager()
    {
        _currentTheme = Theme.Light();
        _styleSheet = new StyleSheet(_currentTheme);
    }

    /// <summary>
    /// 当前主题
    /// </summary>
    public ThemeData CurrentTheme => _currentTheme;

    /// <summary>
    /// 当前样式表
    /// </summary>
    public StyleSheet StyleSheet => _styleSheet;

    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="theme">主题数据</param>
    public void SetTheme(ThemeData theme)
    {
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        _currentTheme = theme;
        _styleSheet = new StyleSheet(theme);

        // 通知所有监听者
        NotifyThemeChanged();
    }

    /// <summary>
    /// 根据名称设置主题
    /// </summary>
    /// <param name="themeName">主题名称（light, dark, material, material-dark, fluent, fluent-dark）</param>
    public void SetTheme(string themeName)
    {
        var theme = Theme.FromName(themeName);
        SetTheme(theme);
    }

    /// <summary>
    /// 切换到浅色主题
    /// </summary>
    public void SetLightTheme() => SetTheme(Theme.Light());

    /// <summary>
    /// 切换到深色主题
    /// </summary>
    public void SetDarkTheme() => SetTheme(Theme.Dark());

    /// <summary>
    /// 切换浅色/深色主题
    /// </summary>
    public void ToggleTheme()
    {
        if (_currentTheme.IsDark)
        {
            SetLightTheme();
        }
        else
        {
            SetDarkTheme();
        }
    }

    /// <summary>
    /// 注册主题变化监听器
    /// </summary>
    /// <param name="listener">监听器回调</param>
    public void OnThemeChanged(Action<ThemeData> listener)
    {
        if (listener != null && !_themeChangeListeners.Contains(listener))
        {
            _themeChangeListeners.Add(listener);
        }
    }

    /// <summary>
    /// 移除主题变化监听器
    /// </summary>
    /// <param name="listener">监听器回调</param>
    public void RemoveThemeChangeListener(Action<ThemeData> listener)
    {
        _themeChangeListeners.Remove(listener);
    }

    /// <summary>
    /// 清除所有监听器
    /// </summary>
    public void ClearListeners()
    {
        _themeChangeListeners.Clear();
    }

    /// <summary>
    /// 通知主题变化
    /// </summary>
    private void NotifyThemeChanged()
    {
        foreach (var listener in _themeChangeListeners)
        {
            try
            {
                listener.Invoke(_currentTheme);
            }
            catch (Exception ex)
            {
                // 记录异常但不中断通知流程
                Console.WriteLine($"Error in theme change listener: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 注册自定义样式到当前样式表
    /// </summary>
    /// <param name="name">样式名称</param>
    /// <param name="style">样式对象</param>
    public void RegisterStyle(string name, Style style)
    {
        _styleSheet.Register(name, style);
    }

    /// <summary>
    /// 获取样式
    /// </summary>
    /// <param name="name">样式名称</param>
    public Style? GetStyle(string name)
    {
        return _styleSheet.Get(name);
    }

    /// <summary>
    /// 获取所有可用主题名称
    /// </summary>
    public static string[] GetAvailableThemes() => Theme.GetAvailableThemes();

    /// <summary>
    /// 重置为默认主题
    /// </summary>
    public void Reset()
    {
        SetTheme(Theme.Light());
    }
}
