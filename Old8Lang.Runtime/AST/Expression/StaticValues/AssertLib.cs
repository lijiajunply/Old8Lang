using System.Collections;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// 断言库 - 提供测试断言功能
/// </summary>
public static class AssertLib
{
    /// <summary>
    /// 断言失败时抛出的异常
    /// </summary>
    public class AssertionException(string message) : Exception(message);

    // ===== 基础相等性断言 =====

    /// <summary>
    /// 断言两个值相等
    /// </summary>
    /// <param name="expected">期望值</param>
    /// <param name="actual">实际值</param>
    /// <param name="message">可选的错误信息</param>
    public static void AssertEqual(object? expected, object? actual, string? message = null)
    {
        if (!AreEqual(expected, actual))
        {
            var msg = message ?? $"断言失败: 期望值 '{expected}' 但实际为 '{actual}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言两个值不相等
    /// </summary>
    /// <param name="notExpected">不期望的值</param>
    /// <param name="actual">实际值</param>
    /// <param name="message">可选的错误信息</param>
    public static void AssertNotEqual(object? notExpected, object? actual, string? message = null)
    {
        if (AreEqual(notExpected, actual))
        {
            var msg = message ?? $"断言失败: 期望值不为 '{notExpected}' 但实际相等";
            throw new AssertionException(msg);
        }
    }

    // ===== 布尔断言 =====

    /// <summary>
    /// 断言条件为真
    /// </summary>
    /// <param name="condition">条件</param>
    /// <param name="message">可选的错误信息</param>
    public static void AssertTrue(bool condition, string? message = null)
    {
        if (!condition)
        {
            var msg = message ?? "断言失败: 期望为 true 但实际为 false";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言条件为假
    /// </summary>
    /// <param name="condition">条件</param>
    /// <param name="message">可选的错误信息</param>
    public static void AssertFalse(bool condition, string? message = null)
    {
        if (condition)
        {
            var msg = message ?? "断言失败: 期望为 false 但实际为 true";
            throw new AssertionException(msg);
        }
    }

    // ===== Null 检查断言 =====

    /// <summary>
    /// 断言值为 null
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="message">可选的错误信息</param>
    public static void AssertNull(object? value, string? message = null)
    {
        if (value != null)
        {
            var msg = message ?? $"断言失败: 期望为 null 但实际为 '{value}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言值不为 null
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="message">可选的错误信息</param>
    public static void AssertNotNull(object? value, string? message = null)
    {
        if (value == null)
        {
            var msg = message ?? "断言失败: 期望不为 null 但实际为 null";
            throw new AssertionException(msg);
        }
    }

    // ===== 数值比较断言 =====

    /// <summary>
    /// 断言第一个值大于第二个值
    /// </summary>
    public static void AssertGreater(IComparable value, IComparable other, string? message = null)
    {
        if (value.CompareTo(other) <= 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' > '{other}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言第一个值大于等于第二个值
    /// </summary>
    public static void AssertGreaterOrEqual(IComparable value, IComparable other, string? message = null)
    {
        if (value.CompareTo(other) < 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' >= '{other}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言第一个值小于第二个值
    /// </summary>
    public static void AssertLess(IComparable value, IComparable other, string? message = null)
    {
        if (value.CompareTo(other) >= 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' < '{other}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言第一个值小于等于第二个值
    /// </summary>
    public static void AssertLessOrEqual(IComparable value, IComparable other, string? message = null)
    {
        if (value.CompareTo(other) > 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' <= '{other}'";
            throw new AssertionException(msg);
        }
    }

    // ===== 字符串断言 =====

    /// <summary>
    /// 断言字符串包含子串
    /// </summary>
    public static void AssertContains(string text, string substring, string? message = null)
    {
        if (!text.Contains(substring))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 不包含 '{substring}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言字符串不包含子串
    /// </summary>
    public static void AssertNotContains(string text, string substring, string? message = null)
    {
        if (text.Contains(substring))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 包含 '{substring}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言字符串以指定前缀开头
    /// </summary>
    public static void AssertStartsWith(string text, string prefix, string? message = null)
    {
        if (!text.StartsWith(prefix))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 不以 '{prefix}' 开头";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言字符串以指定后缀结尾
    /// </summary>
    public static void AssertEndsWith(string text, string suffix, string? message = null)
    {
        if (!text.EndsWith(suffix))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 不以 '{suffix}' 结尾";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言字符串匹配正则表达式
    /// </summary>
    public static void AssertMatches(string text, string pattern, string? message = null)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(text, pattern))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 不匹配正则表达式 '{pattern}'";
            throw new AssertionException(msg);
        }
    }

    // ===== 集合断言 =====

    /// <summary>
    /// 断言集合包含指定元素
    /// </summary>
    public static void AssertContainsItem(IEnumerable collection, object item, string? message = null)
    {
        foreach (var element in collection)
        {
            if (AreEqual(element, item)) return;
        }

        var msg = message ?? $"断言失败: 集合不包含元素 '{item}'";
        throw new AssertionException(msg);
    }

    /// <summary>
    /// 断言集合不包含指定元素
    /// </summary>
    public static void AssertNotContainsItem(IEnumerable collection, object item, string? message = null)
    {
        foreach (var element in collection)
        {
            if (AreEqual(element, item))
            {
                var msg = message ?? $"断言失败: 集合包含元素 '{item}'";
                throw new AssertionException(msg);
            }
        }
    }

    /// <summary>
    /// 断言集合为空
    /// </summary>
    public static void AssertEmpty(IEnumerable collection, string? message = null)
    {
        foreach (var _ in collection)
        {
            var msg = message ?? "断言失败: 集合不为空";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言集合不为空
    /// </summary>
    public static void AssertNotEmpty(IEnumerable collection, string? message = null)
    {
        foreach (var _ in collection)
        {
            return;
        }

        var msg = message ?? "断言失败: 集合为空";
        throw new AssertionException(msg);
    }

    /// <summary>
    /// 断言集合长度
    /// </summary>
    public static void AssertLength(IEnumerable collection, int expectedLength, string? message = null)
    {
        var count = 0;
        foreach (var _ in collection) count++;

        if (count != expectedLength)
        {
            var msg = message ?? $"断言失败: 期望集合长度为 {expectedLength} 但实际为 {count}";
            throw new AssertionException(msg);
        }
    }

    // ===== 异常断言 =====

    /// <summary>
    /// 断言执行指定操作会抛出异常
    /// </summary>
    public static void AssertThrows(Action action, string? message = null)
    {
        try
        {
            action();
            var msg = message ?? "断言失败: 期望抛出异常但未抛出";
            throw new AssertionException(msg);
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception)
        {
            // 预期的异常
        }
    }

    /// <summary>
    /// 断言执行指定操作不会抛出异常
    /// </summary>
    public static void AssertNotThrows(Action action, string? message = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var msg = message ?? $"断言失败: 期望不抛出异常但抛出了 {ex.GetType().Name}: {ex.Message}";
            throw new AssertionException(msg);
        }
    }

    // ===== 类型断言 =====

    /// <summary>
    /// 断言对象是指定类型
    /// </summary>
    public static void AssertInstanceOf(object? obj, Type expectedType, string? message = null)
    {
        if (obj == null || !expectedType.IsInstanceOfType(obj))
        {
            var actualType = obj?.GetType().Name ?? "null";
            var msg = message ?? $"断言失败: 期望类型为 '{expectedType.Name}' 但实际为 '{actualType}'";
            throw new AssertionException(msg);
        }
    }

    /// <summary>
    /// 断言对象不是指定类型
    /// </summary>
    public static void AssertNotInstanceOf(object? obj, Type unexpectedType, string? message = null)
    {
        if (obj != null && unexpectedType.IsInstanceOfType(obj))
        {
            var msg = message ?? $"断言失败: 期望类型不为 '{unexpectedType.Name}' 但实际为该类型";
            throw new AssertionException(msg);
        }
    }

    // ===== 辅助方法 =====

    /// <summary>
    /// 比较两个对象是否相等
    /// </summary>
    private static bool AreEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;

        // 处理集合类型
        if (a is IEnumerable enumA && b is IEnumerable enumB)
        {
            return CollectionsEqual(enumA, enumB);
        }

        return a.Equals(b);
    }

    /// <summary>
    /// 比较两个集合是否相等
    /// </summary>
    private static bool CollectionsEqual(IEnumerable a, IEnumerable b)
    {
        var listA = a.Cast<object?>().ToList();
        var listB = b.Cast<object?>().ToList();

        if (listA.Count != listB.Count) return false;

        for (int i = 0; i < listA.Count; i++)
        {
            if (!AreEqual(listA[i], listB[i])) return false;
        }

        return true;
    }
}