using System.Text.RegularExpressions;

namespace Old8LangLib;

/// <summary>
/// 字符串处理模块，用于各种字符串操作
/// </summary>
public static class StringLib
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

    /// <summary>
    /// 格式化字符串
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="args">格式参数</param>
    /// <returns>格式化后的字符串</returns>
    public static string Format(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format))
        {
            throw new ArgumentNullException(nameof(format), "格式字符串不能为空");
        }

        if (args == null)
        {
            throw new ArgumentNullException(nameof(args), "格式参数不能为空");
        }

        try
        {
            return string.Format(format, args);
        }
        catch (Exception ex)
        {
            throw new FormatException($"字符串格式化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将字符串转换为Base64编码
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>Base64编码的字符串</returns>
    public static string ToBase64(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        try
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            throw new EncodingException($"Base64编码失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将Base64编码的字符串转换为原始字符串
    /// </summary>
    /// <param name="input">Base64编码的字符串</param>
    /// <returns>原始字符串</returns>
    public static string FromBase64(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(input);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            throw new EncodingException($"Base64解码失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将字符串转换为大写
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>大写字符串</returns>
    public static string ToUpper(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        return input.ToUpper();
    }

    /// <summary>
    /// 将字符串转换为小写
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>小写字符串</returns>
    public static string ToLower(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        return input.ToLower();
    }

    /// <summary>
    /// 去除字符串两端的空白字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>去除空白字符后的字符串</returns>
    public static string Trim(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        return input.Trim();
    }

    /// <summary>
    /// 去除字符串左侧的空白字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>去除左侧空白字符后的字符串</returns>
    public static string TrimStart(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        return input.TrimStart();
    }

    /// <summary>
    /// 去除字符串右侧的空白字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>去除右侧空白字符后的字符串</returns>
    public static string TrimEnd(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        return input.TrimEnd();
    }

    /// <summary>
    /// 检查字符串是否以指定的前缀开头
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="prefix">前缀</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false</param>
    /// <returns>如果以指定前缀开头则返回true，否则返回false</returns>
    public static bool StartsWith(string input, string prefix, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentNullException(nameof(prefix), "前缀不能为空");
        }

        return input.StartsWith(prefix, ignoreCase, null);
    }

    /// <summary>
    /// 检查字符串是否以指定的后缀结尾
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="suffix">后缀</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false</param>
    /// <returns>如果以指定后缀结尾则返回true，否则返回false</returns>
    public static bool EndsWith(string input, string suffix, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(suffix))
        {
            throw new ArgumentNullException(nameof(suffix), "后缀不能为空");
        }

        return input.EndsWith(suffix, ignoreCase, null);
    }

    /// <summary>
    /// 在字符串中查找子字符串的位置
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="substring">子字符串</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为false</param>
    /// <returns>子字符串的位置，如果没有找到则返回-1</returns>
    public static int IndexOf(string input, string substring, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(substring))
        {
            throw new ArgumentNullException(nameof(substring), "子字符串不能为空");
        }

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.IndexOf(substring, comparison);
    }

    /// <summary>
    /// 拆分字符串为数组
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="separator">分隔符</param>
    /// <returns>拆分后的字符串数组</returns>
    public static string[] Split(string input, string separator)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (string.IsNullOrEmpty(separator))
        {
            throw new ArgumentNullException(nameof(separator), "分隔符不能为空");
        }

        return input.Split([separator], StringSplitOptions.None);
    }

    /// <summary>
    /// 连接字符串数组为单个字符串
    /// </summary>
    /// <param name="input">字符串数组</param>
    /// <param name="separator">分隔符</param>
    /// <returns>连接后的字符串</returns>
    public static string Join(string[] input, string separator)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input), "字符串数组不能为空");
        }

        return string.Join(separator, input);
    }

    /// <summary>
    /// 重复字符串指定次数
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="count">重复次数</param>
    /// <returns>重复后的字符串</returns>
    public static string Repeat(string input, int count)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "输入字符串不能为空");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "重复次数不能为负数");
        }

        return new string(input[0], count);
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
    public RegexException() : base()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public RegexException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public RegexException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 编码异常类
/// </summary>
public class EncodingException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public EncodingException() : base()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EncodingException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EncodingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}