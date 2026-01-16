namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// object[] 类型的扩展方法类
/// 为虚拟机模式和编译器模式中的原生 C# 数组提供 Old8Lang 风格的方法
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// 获取数组长度
    /// </summary>
    public static int Length(this object?[] array)
    {
        return array.Length;
    }

    /// <summary>
    /// 检查数组是否包含指定元素
    /// </summary>
    public static bool Contains(this object?[] array, object? item)
    {
        return array.Contains(item);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    public static string ToStr(this object?[] array)
    {
        var items = array.Select(item => item?.ToString() ?? "null");
        return "[" + string.Join(", ", items) + "]";
    }

    /// <summary>
    /// 转换为列表
    /// </summary>
    public static List<object?> ToList(this object?[] array)
    {
        return array.ToList();
    }
}
