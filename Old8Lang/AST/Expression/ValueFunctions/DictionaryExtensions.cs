using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// Dictionary<object, object?> 类型的扩展方法类
/// 为虚拟机模式中的原生 C# Dictionary 提供 Old8Lang 风格的方法
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// 检查字典是否包含指定键
    /// </summary>
    public static bool ContainsKey(this Dictionary<object, object?> dict, object key)
    {
        return dict.ContainsKey(key);
    }

    /// <summary>
    /// 检查字典是否包含指定值
    /// </summary>
    public static bool ContainsValue(this Dictionary<object, object?> dict, object? value)
    {
        return dict.ContainsValue(value);
    }

    /// <summary>
    /// 获取字典中的值
    /// </summary>
    public static object? Get(this Dictionary<object, object?> dict, object key)
    {
        return dict.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// 设置字典中的值
    /// </summary>
    public static void Set(this Dictionary<object, object?> dict, object key, object? value)
    {
        dict[key] = value;
    }

    /// <summary>
    /// 移除字典中的键值对
    /// </summary>
    public static bool Remove(this Dictionary<object, object?> dict, object key)
    {
        return dict.Remove(key);
    }

    /// <summary>
    /// 返回字典元素数量
    /// </summary>
    public static int Count(this Dictionary<object, object?> dict)
    {
        return dict.Count;
    }

    /// <summary>
    /// 清空字典
    /// </summary>
    public static void Clear(this Dictionary<object, object?> dict)
    {
        dict.Clear();
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    public static string ToStr(this Dictionary<object, object?> dict)
    {
        var items = dict.Select(kvp => $"{kvp.Key}: {kvp.Value?.ToString() ?? "null"}");
        return "{" + string.Join(", ", items) + "}";
    }
}
