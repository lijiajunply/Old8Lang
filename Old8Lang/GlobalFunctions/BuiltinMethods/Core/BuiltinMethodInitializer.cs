using Old8Lang.AST.Expression.Value;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.Core;

/// <summary>
/// 内置方法初始化器 - 负责注册所有内置方法到注册表
/// </summary>
/// <remarks>
/// 使用延迟初始化和双重检查锁定模式，确保线程安全且只初始化一次。
/// </remarks>
public static class BuiltinMethodInitializer
{
    private static bool _initialized;
    private static readonly Lock InitLock = new();

    /// <summary>
    /// 初始化所有内置方法
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        lock (InitLock)
        {
            if (_initialized) return;

            var registry = BuiltinMethodRegistry.Instance;

            // 注册 List 方法
            RegisterListMethods(registry);

            // 注册 String 方法
            RegisterStringMethods(registry);

            // 注册 Dictionary 方法
            RegisterDictionaryMethods(registry);

            // 注册 Array 方法
            RegisterArrayMethods(registry);

            _initialized = true;
        }
    }

    /// <summary>
    /// 确保已初始化
    /// </summary>
    public static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// 注册 List 相关方法
    /// </summary>
    private static void RegisterListMethods(BuiltinMethodRegistry registry)
    {
        // ===== 基础方法 =====
        // List.Add(element) - 添加元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Add",
            new List.ListAddFunction());

        // List.Count() - 获取元素数量
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Count",
            new List.ListCountFunction());

        // List.Remove(element) - 移除指定元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Remove",
            new List.ListRemoveFunction());

        // List.RemoveAt(index) - 根据索引移除元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "RemoveAt",
            new List.ListRemoveAtFunction());

        // List.AddList(otherList) - 添加另一个列表的所有元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "AddList",
            new List.ListAddListFunction());

        // List.Clear() - 清空列表
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Clear",
            new List.ListClearFunction());

        // List.Insert(index, element) - 在指定位置插入元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Insert",
            new List.ListInsertFunction());

        // List.Pop() - 移除并返回最后一个元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Pop",
            new List.ListPopFunction());

        // ===== 查询方法 =====
        // List.Contains(element) - 检查是否包含元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Contains",
            new List.ListContainsFunction());

        // List.IndexOf(element) - 查找元素索引
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "IndexOf",
            new List.ListIndexOfFunction());

        // List.IsEmpty() - 检查列表是否为空
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "IsEmpty",
            new List.ListIsEmptyFunction());

        // List.First() - 获取第一个元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "First",
            new List.ListFirstFunction());

        // List.FirstOrDefault() - 获取第一个元素或 null
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "FirstOrDefault",
            new List.ListFirstOrDefaultFunction());

        // List.Last() - 获取最后一个元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Last",
            new List.ListLastFunction());

        // List.LastOrDefault() - 获取最后一个元素或 null
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "LastOrDefault",
            new List.ListLastOrDefaultFunction());

        // List.ElementAtOrDefault(index) - 获取指定索引的元素或 null
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "ElementAtOrDefault",
            new List.ListElementAtOrDefaultFunction());

        // ===== 转换方法 =====
        // List.Reverse() - 反转列表
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Reverse",
            new List.ListReverseFunction());

        // List.Concat(otherList) - 连接两个列表
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Concat",
            new List.ListConcatFunction());

        // List.ToArray() - 转换为数组
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "ToArray",
            new List.ListToArrayFunction());

        // List.Slice(start, end) - 获取切片
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Slice",
            new List.ListSliceFunction());

        // List.Join(separator) - 连接为字符串
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Join",
            new List.ListJoinFunction());

        // List.Distinct() - 去重
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Distinct",
            new List.ListDistinctFunction());

        // List.Take(count) - 获取前 n 个元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Take",
            new List.ListTakeFunction());

        // List.Skip(count) - 跳过前 n 个元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Skip",
            new List.ListSkipFunction());

        // ===== 高阶函数方法 =====
        // List.Any() - 检查列表是否不为空
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Any",
            new List.ListAnyFunction());

        // ===== 集合操作方法 =====
        // List.Union(otherList) - 并集
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Union",
            new List.ListUnionFunction());

        // List.Intersect(otherList) - 交集
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Intersect",
            new List.ListIntersectFunction());

        // List.Except(otherList) - 差集
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Except",
            new List.ListExceptFunction());

        // ===== 排序方法 =====
        // List.Sort() - 排序
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Sort",
            new List.ListSortFunction());

        // List.IsSorted() - 检查是否已排序
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "IsSorted",
            new List.ListIsSortedFunction());

        // ===== 数学方法 =====
        // List.Sum() - 求和
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Sum",
            new List.ListSumFunction());

        // List.Average() - 平均值
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Average",
            new List.ListAverageFunction());

        // List.Min() - 最小值
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Min",
            new List.ListMinFunction());

        // List.Max() - 最大值
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Max",
            new List.ListMaxFunction());

        // ===== 高级方法 =====
        // List.TakeLast(count) - 获取最后 n 个元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "TakeLast",
            new List.ListTakeLastFunction());

        // List.SkipLast(count) - 跳过最后 n 个元素
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "SkipLast",
            new List.ListSkipLastFunction());

        // List.Chunk(size) - 分块
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Chunk",
            new List.ListChunkFunction());

        // List.Flatten() - 展平
        registry.Register(
            typeof(ListLangValue),
            typeof(List<object>),
            "Flatten",
            new List.ListFlattenFunction());
    }

    /// <summary>
    /// 注册 String 相关方法
    /// </summary>
    private static void RegisterStringMethods(BuiltinMethodRegistry registry)
    {
        // String.Length() - 获取字符串长度
        registry.Register(
            typeof(StringLangValue),
            typeof(string),
            "Length",
            new String.StringLengthFunction());
    }

    /// <summary>
    /// 注册 Dictionary 相关方法
    /// </summary>
    private static void RegisterDictionaryMethods(BuiltinMethodRegistry registry)
    {
        // 后续阶段实现
    }

    /// <summary>
    /// 注册 Array 相关方法
    /// </summary>
    private static void RegisterArrayMethods(BuiltinMethodRegistry registry)
    {
        // 后续阶段实现
    }

    /// <summary>
    /// 重置初始化状态（主要用于测试）
    /// </summary>
    public static void Reset()
    {
        lock (InitLock)
        {
            _initialized = false;
            BuiltinMethodRegistry.Instance.Clear();
        }
    }
}
