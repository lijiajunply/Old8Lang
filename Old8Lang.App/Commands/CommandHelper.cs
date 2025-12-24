using Old8Lang.ProjectManagement;

namespace Old8Lang.App.Commands;

/// <summary>
/// 命令工具类
/// </summary>
public static class CommandHelper
{
    /// <summary>
    /// 查找项目根目录
    /// </summary>
    public static string? FindProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        return ProjectConfig.FindProjectRoot(currentDir);
    }

    /// <summary>
    /// 加载项目配置
    /// </summary>
    public static ProjectConfig? LoadProjectConfig()
    {
        var projectRoot = FindProjectRoot();
        if (projectRoot == null)
            return null;

        return ProjectConfig.LoadFromDirectory(projectRoot);
    }

    /// <summary>
    /// 打印成功消息
    /// </summary>
    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// 打印错误消息
    /// </summary>
    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// 打印警告消息
    /// </summary>
    public static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// 打印信息消息
    /// </summary>
    public static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// 从用户读取输入
    /// </summary>
    public static string? ReadLine(string prompt, string? defaultValue = null)
    {
        if (defaultValue != null)
        {
            Console.Write($"{prompt} ({defaultValue}): ");
        }
        else
        {
            Console.Write($"{prompt}: ");
        }

        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }

    /// <summary>
    /// 从用户读取是/否
    /// </summary>
    public static bool ReadYesNo(string prompt, bool defaultValue = true)
    {
        var defaultStr = defaultValue ? "Y/n" : "y/N";
        Console.Write($"{prompt} ({defaultStr}): ");

        var input = Console.ReadLine()?.Trim().ToLower();

        if (string.IsNullOrEmpty(input))
            return defaultValue;

        return input == "y" || input == "yes";
    }
}
