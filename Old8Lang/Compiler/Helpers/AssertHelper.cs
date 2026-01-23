namespace Old8Lang.Compiler.Helpers;

/// <summary>
/// Assert辅助类，提供编译模式下断言方法的实际实现
/// </summary>
/// <remarks>
/// 该类将被编译后的IL代码调用，提供与解释模式相同的断言功能。
/// 所有方法都是静态的，以便在IL代码中直接调用。
/// </remarks>
public static class AssertHelper
{
    /// <summary>
    /// 断言两个值相等（带消息）
    /// </summary>
    public static void AssertEqual(object expected, object actual, string? message = null)
    {
        if (!AreEqual(expected, actual))
        {
            var msg = message ?? $"断言失败: 期望值 '{expected}' 但实际为 '{actual}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言两个值不相等（带消息）
    /// </summary>
    public static void AssertNotEqual(object notExpected, object actual, string? message = null)
    {
        if (AreEqual(notExpected, actual))
        {
            var msg = message ?? $"断言失败: 期望值不为 '{notExpected}' 但实际相等";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言条件为真（带消息）
    /// </summary>
    public static void AssertTrue(bool condition, string? message = null)
    {
        if (!condition)
        {
            var msg = message ?? "断言失败: 期望为 true 但实际为 false";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言条件为假（带消息）
    /// </summary>
    public static void AssertFalse(bool condition, string? message = null)
    {
        if (condition)
        {
            var msg = message ?? "断言失败: 期望为 false 但实际为 true";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言值为null（带消息）
    /// </summary>
    public static void AssertNull(object? value, string? message = null)
    {
        if (value is not null)
        {
            var msg = message ?? $"断言失败: 期望为 null 但实际为 '{value}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言值不为null（带消息）
    /// </summary>
    public static void AssertNotNull(object? value, string? message = null)
    {
        if (value is null)
        {
            var msg = message ?? "断言失败: 期望不为 null 但实际为 null";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言第一个值大于第二个值（带消息）
    /// </summary>
    public static void AssertGreater(object value, object other, string? message = null)
    {
        if (!TryCompare(value, other, out var compareResult) || compareResult <= 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' > '{other}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言第一个值大于等于第二个值（带消息）
    /// </summary>
    public static void AssertGreaterOrEqual(object value, object other, string? message = null)
    {
        if (!TryCompare(value, other, out var compareResult) || compareResult < 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' >= '{other}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言第一个值小于第二个值（带消息）
    /// </summary>
    public static void AssertLess(object value, object other, string? message = null)
    {
        if (!TryCompare(value, other, out var compareResult) || compareResult >= 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' < '{other}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言第一个值小于等于第二个值（带消息）
    /// </summary>
    public static void AssertLessOrEqual(object value, object other, string? message = null)
    {
        if (!TryCompare(value, other, out var compareResult) || compareResult > 0)
        {
            var msg = message ?? $"断言失败: 期望 '{value}' <= '{other}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言字符串包含子串（带消息）
    /// </summary>
    public static void AssertContains(string text, string substring, string? message = null)
    {
        if (!text.Contains(substring))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 不包含 '{substring}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言字符串不包含子串（带消息）
    /// </summary>
    public static void AssertNotContains(string text, string substring, string? message = null)
    {
        if (text.Contains(substring))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 包含 '{substring}'";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言字符串以指定前缀开头（带消息）
    /// </summary>
    public static void AssertStartsWith(string text, string prefix, string? message = null)
    {
        if (!text.StartsWith(prefix))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 不以 '{prefix}' 开头";
            throw new Exception(msg);
        }
    }

    /// <summary>
    /// 断言字符串以指定后缀结尾（带消息）
    /// </summary>
    public static void AssertEndsWith(string text, string suffix, string? message = null)
    {
        if (!text.EndsWith(suffix))
        {
            var msg = message ?? $"断言失败: 字符串 '{text}' 不以 '{suffix}' 结尾";
            throw new Exception(msg);
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 比较两个对象是否相等
    /// </summary>
    private static bool AreEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        // 处理字符串比较
        if (a is string strA && b is string strB)
            return strA == strB;

        // 处理数值比较
        if (a is int intA && b is int intB)
            return intA == intB;

        if (a is double doubleA && b is double doubleB)
            return Math.Abs(doubleA - doubleB) < 1e-10;

        if (a is double doubleA2 && b is int intB2)
            return Math.Abs(doubleA2 - intB2) < 1e-10;

        if (a is int intA2 && b is double doubleB2)
            return Math.Abs(intA2 - doubleB2) < 1e-10;

        // 处理布尔比较
        if (a is bool boolA && b is bool boolB)
            return boolA == boolB;

        // 默认使用ToString比较
        return a.ToString() == b.ToString();
    }

    /// <summary>
    /// 尝试比较两个值，返回比较结果
    /// </summary>
    private static bool TryCompare(object a, object b, out int result)
    {
        result = 0;

        // 处理数值比较
        if (a is int intA)
        {
            if (b is int intB)
            {
                result = intA.CompareTo(intB);
                return true;
            }

            if (b is double doubleB)
            {
                result = intA.CompareTo(doubleB);
                return true;
            }
        }

        if (a is double doubleA)
        {
            if (b is int intB)
            {
                result = doubleA.CompareTo(intB);
                return true;
            }

            if (b is double doubleB)
            {
                result = doubleA.CompareTo(doubleB);
                return true;
            }
        }

        // 处理字符串比较
        if (a is string strA && b is string strB)
        {
            result = string.Compare(strA, strB, StringComparison.Ordinal);
            return true;
        }

        return false;
    }

    #endregion
}