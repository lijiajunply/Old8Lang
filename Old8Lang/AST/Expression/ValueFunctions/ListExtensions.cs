namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// List<object?> 类型的扩展方法类
/// 为虚拟机模式中的原生 C# List 提供 Old8Lang 风格的方法
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// 向列表中添加元素
    /// </summary>
    public static object? Add(this List<object?> list, object? item)
    {
        list.Add(item);
        return item;
    }

    /// <summary>
    /// 从列表中移除指定元素
    /// </summary>
    public static object? Remove(this List<object?> list, object? item)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (Equals(list[i], item))
            {
                var removed = list[i];
                list.RemoveAt(i);
                return removed;
            }
        }
        throw new Exception("找不到要移除的元素");
    }

    /// <summary>
    /// 根据索引从列表中移除元素
    /// </summary>
    public static object? RemoveAt(this List<object?> list, int index)
    {
        var item = list[index];
        list.RemoveAt(index);
        return item;
    }

    /// <summary>
    /// 返回列表元素数量
    /// </summary>
    public static int Count(this List<object?> list)
    {
        return list.Count;
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    public static void Clear(this List<object?> list)
    {
        list.Clear();
    }

    /// <summary>
    /// 检查列表是否包含指定元素
    /// </summary>
    public static bool Contains(this List<object?> list, object? item)
    {
        return list.Contains(item);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    public static string ToStr(this List<object?> list)
    {
        var items = list.Select(item => item?.ToString() ?? "null");
        return "[" + string.Join(", ", items) + "]";
    }
}
