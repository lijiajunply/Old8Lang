namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// string 类型的扩展方法类
/// 为虚拟机模式和编译器模式中的原生 C# string 提供 Old8Lang 风格的方法
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 转换为大写
    /// </summary>
    public static string Upper(this string str)
    {
        return str.ToUpper();
    }

    /// <summary>
    /// 转换为小写
    /// </summary>
    public static string Lower(this string str)
    {
        return str.ToLower();
    }

    /// <summary>
    /// 获取字符串长度
    /// </summary>
    public static int Length(this string str)
    {
        return str.Length;
    }

    /// <summary>
    /// 检查字符串是否包含子串
    /// </summary>
    public static bool Contains(this string str, string substring)
    {
        return str.Contains(substring);
    }

    /// <summary>
    /// 替换字符串
    /// </summary>
    public static string Replace(this string str, string oldValue, string newValue)
    {
        return str.Replace(oldValue, newValue);
    }

    /// <summary>
    /// 分割字符串
    /// </summary>
    public static List<object?> Split(this string str, string separator)
    {
        var parts = str.Split(separator);
        return parts.Cast<object?>().ToList();
    }

    /// <summary>
    /// 去除首尾空白
    /// </summary>
    public static string Trim(this string str)
    {
        return str.Trim();
    }

    /// <summary>
    /// 获取子串
    /// </summary>
    public static string Substring(this string str, int startIndex, int length)
    {
        return str.Substring(startIndex, length);
    }

    /// <summary>
    /// 转换为字符串表示（返回自身）
    /// </summary>
    public static string ToStr(this string str)
    {
        return str;
    }
}
