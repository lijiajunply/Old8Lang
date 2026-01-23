namespace Old8Lang.Compiler.Helpers;

/// <summary>
/// 集合操作辅助类，提供编译模式下集合操作的实际实现
/// </summary>
/// <remarks>
/// 该类将被编译后的IL代码调用，提供与解释模式相同的集合操作功能。
/// 所有方法都是静态的，以便在IL代码中直接调用。
/// </remarks>
public static class CollectionHelper
{
    /// <summary>
    /// 对数组执行切片操作
    /// </summary>
    /// <param name="array">源数组</param>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <param name="step">步长</param>
    /// <returns>切片后的新数组</returns>
    public static object[] SliceArray(object[] array, int start, int end, int step)
    {
        var length = array.Length;
        var result = new List<object>();

        if (step > 0)
        {
            // 正向切片
            if (start < 0) start += length;
            if (end < 0) end += length;

            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            for (int i = start; i < end; i += step)
            {
                result.Add(array[i]);
            }
        }
        else if (step < 0)
        {
            // 反向切片
            if (start < -1) start += length;
            if (end < -1) end += length;

            // 设置边界
            if (start >= length) start = length - 1;
            if (start < -1) start = -1;
            if (end >= length) end = length - 1;

            for (int i = start; i > end; i += step)
            {
                result.Add(array[i]);
            }
        }
        else
        {
            throw new InvalidOperationException("切片步长不能为0");
        }

        return result.ToArray();
    }

    /// <summary>
    /// 对列表执行切片操作
    /// </summary>
    /// <param name="list">源列表</param>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <param name="step">步长</param>
    /// <returns>切片后的新列表</returns>
    public static List<object> SliceList(List<object> list, int start, int end, int step)
    {
        var length = list.Count;
        var result = new List<object>();

        if (step > 0)
        {
            // 正向切片
            if (start < 0) start += length;
            if (end < 0) end += length;

            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            for (int i = start; i < end; i += step)
            {
                result.Add(list[i]);
            }
        }
        else if (step < 0)
        {
            // 反向切片
            if (start < -1) start += length;
            if (end < -1) end += length;

            // 设置边界
            if (start >= length) start = length - 1;
            if (start < -1) start = -1;
            if (end >= length) end = length - 1;

            for (int i = start; i > end; i += step)
            {
                result.Add(list[i]);
            }
        }
        else
        {
            throw new InvalidOperationException("切片步长不能为0");
        }

        return result;
    }

    /// <summary>
    /// 通用切片操作，自动识别集合类型
    /// </summary>
    /// <param name="collection">源集合（数组或列表）</param>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <param name="step">步长</param>
    /// <returns>切片后的新集合</returns>
    public static object Slice(object collection, int start, int end, int step)
    {
        if (collection is object[] array)
        {
            return SliceArray(array, start, end, step);
        }
        else if (collection is List<object> list)
        {
            return SliceList(list, start, end, step);
        }
        else
        {
            throw new InvalidOperationException($"不支持的切片类型: {collection.GetType().Name}");
        }
    }
}
