using System.Text.RegularExpressions;

namespace Old8LangLib;

[Serializable]
public class RegexLib
{
    /// <summary>
    /// 检查字符串是否匹配正则表达式
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false</param>
    /// <returns>如果匹配则返回true，否则返回false</returns>
    public static bool RegexIsMatch(string input, string pattern, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentNullException(nameof(pattern), "正则表达式模式不能为空");
        }

        try
        {
            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.IsMatch(input, pattern, options);
        }
        catch (Exception ex)
        {
            throw new RegexException($"正则表达式匹配失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 使用正则表达式替换字符串
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="replacement">替换字符串</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false</param>
    /// <returns>替换后的字符串</returns>
    public static string RegexReplace(string input, string pattern, string replacement, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentNullException(nameof(pattern), "正则表达式模式不能为空");
        }

        try
        {
            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.Replace(input, pattern, replacement, options);
        }
        catch (Exception ex)
        {
            throw new RegexException($"正则表达式替换失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 使用正则表达式匹配并提取第一个匹配项
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false</param>
    /// <returns>第一个匹配项，如果没有匹配则返回null</returns>
    public static string? RegexMatch(string input, string pattern, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentNullException(nameof(pattern), "正则表达式模式不能为空");
        }

        try
        {
            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            var match = Regex.Match(input, pattern, options);
            return match.Success ? match.Value : null;
        }
        catch (Exception ex)
        {
            throw new RegexException($"正则表达式匹配失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 使用正则表达式匹配并提取所有匹配项
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false</param>
    /// <returns>所有匹配项的数组</returns>
    public static string[] RegexMatches(string input, string pattern, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentNullException(nameof(pattern), "正则表达式模式不能为空");
        }

        try
        {
            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            var matches = Regex.Matches(input, pattern, options);
            return matches.Select(match => match.Value).ToArray();
        }
        catch (Exception ex)
        {
            throw new RegexException($"正则表达式匹配失败: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// 正则表达式异常类
/// </summary>
public class RegexException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常信息</param>
    /// <param name="innerException">内部异常</param>
    public RegexException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常信息</param>
    public RegexException(string message) : base(message)
    {
    }
}