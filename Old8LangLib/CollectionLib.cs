namespace Old8LangLib;

/// <summary>
/// 集合处理模块，用于各种集合操作
/// </summary>
public static class CollectionLib
{
    // ========== 列表操作 ==========

    /// <summary>
    /// 过滤列表中的元素
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="predicate">过滤条件</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>过滤后的列表</returns>
    public static List<T> ListFilter<T>(List<T> list, Func<T, bool> predicate)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate), "过滤条件不能为空");
        }
        
        return list.Where(predicate).ToList();
    }

    /// <summary>
    /// 将列表中的每个元素转换为另一种类型
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="selector">转换函数</param>
    /// <typeparam name="TInput">输入元素类型</typeparam>
    /// <typeparam name="TOutput">输出元素类型</typeparam>
    /// <returns>转换后的列表</returns>
    public static List<TOutput> ListMap<TInput, TOutput>(List<TInput> list, Func<TInput, TOutput> selector)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector), "转换函数不能为空");
        }
        
        return list.Select(selector).ToList();
    }

    /// <summary>
    /// 将列表中的元素折叠为单个值
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="initialValue">初始值</param>
    /// <param name="func">折叠函数</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <returns>折叠后的结果</returns>
    public static TResult ListFold<T, TResult>(List<T> list, TResult initialValue, Func<TResult, T, TResult> func)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func), "折叠函数不能为空");
        }
        
        return list.Aggregate(initialValue, func);
    }

    /// <summary>
    /// 查找列表中的第一个满足条件的元素
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="predicate">查找条件</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>找到的元素，如果没有找到则返回默认值</returns>
    public static T? ListFind<T>(List<T> list, Func<T, bool> predicate)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate), "查找条件不能为空");
        }
        
        return list.FirstOrDefault(predicate);
    }

    /// <summary>
    /// 对列表进行排序
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="comparer">比较器，默认为null</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的列表</returns>
    public static List<T> ListSort<T>(List<T> list, Comparison<T>? comparer = null)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        var result = new List<T>(list);
        
        if (comparer == null)
        {
            result.Sort();
        }
        else
        {
            result.Sort(comparer);
        }
        
        return result;
    }

    /// <summary>
    /// 对列表进行排序（使用指定的键选择器）
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="keySelector">键选择器</param>
    /// <param name="ascending">是否升序，默认为true</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <returns>排序后的列表</returns>
    public static List<T> ListSortBy<T, TKey>(List<T> list, Func<T, TKey> keySelector, bool ascending = true)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector), "键选择器不能为空");
        }
        
        var result = new List<T>(list);
        
        if (ascending)
        {
            result.Sort((a, b) => Comparer<TKey>.Default.Compare(keySelector(a), keySelector(b)));
        }
        else
        {
            result.Sort((a, b) => Comparer<TKey>.Default.Compare(keySelector(b), keySelector(a)));
        }
        
        return result;
    }

    /// <summary>
    /// 反转列表
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>反转后的列表</returns>
    public static List<T> ListReverse<T>(List<T> list)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        var result = new List<T>(list);
        result.Reverse();
        return result;
    }

    /// <summary>
    /// 去重列表
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>去重后的列表</returns>
    public static List<T> ListDistinct<T>(List<T> list)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        return list.Distinct().ToList();
    }

    /// <summary>
    /// 取列表的前N个元素
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="count">元素个数</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>前N个元素的列表</returns>
    public static List<T> ListTake<T>(List<T> list, int count)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "元素个数不能为负数");
        }
        
        return list.Take(count).ToList();
    }

    /// <summary>
    /// 跳过列表的前N个元素
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="count">跳过的元素个数</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>跳过前N个元素后的列表</returns>
    public static List<T> ListSkip<T>(List<T> list, int count)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }
        
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "跳过的元素个数不能为负数");
        }
        
        return list.Skip(count).ToList();
    }

    // ========== 字典操作 ==========

    /// <summary>
    /// 合并两个字典
    /// </summary>
    /// <param name="dict1">第一个字典</param>
    /// <param name="dict2">第二个字典</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>合并后的字典</returns>
    public static Dictionary<TKey, TValue> DictMerge<TKey, TValue>(Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
    {
        if (dict1 == null)
        {
            throw new ArgumentNullException(nameof(dict1), "第一个字典不能为空");
        }
        
        if (dict2 == null)
        {
            throw new ArgumentNullException(nameof(dict2), "第二个字典不能为空");
        }
        
        var result = new Dictionary<TKey, TValue>(dict1);
        foreach (var pair in dict2)
        {
            result[pair.Key] = pair.Value;
        }
        return result;
    }

    /// <summary>
    /// 过滤字典中的键值对
    /// </summary>
    /// <param name="dict">输入字典</param>
    /// <param name="predicate">过滤条件</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>过滤后的字典</returns>
    public static Dictionary<TKey, TValue> DictFilter<TKey, TValue>(Dictionary<TKey, TValue> dict, Func<KeyValuePair<TKey, TValue>, bool> predicate)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict), "输入字典不能为空");
        }
        
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate), "过滤条件不能为空");
        }
        
        return dict.Where(predicate).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    /// <summary>
    /// 将字典转换为列表
    /// </summary>
    /// <param name="dict">输入字典</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>转换后的列表</returns>
    public static List<KeyValuePair<TKey, TValue>> DictToList<TKey, TValue>(Dictionary<TKey, TValue> dict)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict), "输入字典不能为空");
        }
        
        return dict.ToList();
    }

    /// <summary>
    /// 获取字典的所有键
    /// </summary>
    /// <param name="dict">输入字典</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>键列表</returns>
    public static List<TKey> DictKeys<TKey, TValue>(Dictionary<TKey, TValue> dict)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict), "输入字典不能为空");
        }
        
        return dict.Keys.ToList();
    }

    /// <summary>
    /// 获取字典的所有值
    /// </summary>
    /// <param name="dict">输入字典</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>值列表</returns>
    public static List<TValue> DictValues<TKey, TValue>(Dictionary<TKey, TValue> dict)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict), "输入字典不能为空");
        }
        
        return dict.Values.ToList();
    }

    // ========== 集合操作 ==========

    /// <summary>
    /// 计算两个集合的交集
    /// </summary>
    /// <param name="set1">第一个集合</param>
    /// <param name="set2">第二个集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>交集集合</returns>
    public static HashSet<T> SetIntersection<T>(HashSet<T> set1, HashSet<T> set2)
    {
        if (set1 == null)
        {
            throw new ArgumentNullException(nameof(set1), "第一个集合不能为空");
        }
        
        if (set2 == null)
        {
            throw new ArgumentNullException(nameof(set2), "第二个集合不能为空");
        }
        
        var result = new HashSet<T>(set1);
        result.IntersectWith(set2);
        return result;
    }

    /// <summary>
    /// 计算两个集合的并集
    /// </summary>
    /// <param name="set1">第一个集合</param>
    /// <param name="set2">第二个集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>并集集合</returns>
    public static HashSet<T> SetUnion<T>(HashSet<T> set1, HashSet<T> set2)
    {
        if (set1 == null)
        {
            throw new ArgumentNullException(nameof(set1), "第一个集合不能为空");
        }
        
        if (set2 == null)
        {
            throw new ArgumentNullException(nameof(set2), "第二个集合不能为空");
        }
        
        var result = new HashSet<T>(set1);
        result.UnionWith(set2);
        return result;
    }

    /// <summary>
    /// 计算两个集合的差集（set1 - set2）
    /// </summary>
    /// <param name="set1">第一个集合</param>
    /// <param name="set2">第二个集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>差集集合</returns>
    public static HashSet<T> SetDifference<T>(HashSet<T> set1, HashSet<T> set2)
    {
        if (set1 == null)
        {
            throw new ArgumentNullException(nameof(set1), "第一个集合不能为空");
        }
        
        if (set2 == null)
        {
            throw new ArgumentNullException(nameof(set2), "第二个集合不能为空");
        }
        
        var result = new HashSet<T>(set1);
        result.ExceptWith(set2);
        return result;
    }

    /// <summary>
    /// 检查一个集合是否是另一个集合的子集
    /// </summary>
    /// <param name="subset">子集</param>
    /// <param name="superset">超集</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>如果是子集则返回true，否则返回false</returns>
    public static bool SetIsSubsetOf<T>(HashSet<T> subset, HashSet<T> superset)
    {
        if (subset == null)
        {
            throw new ArgumentNullException(nameof(subset), "子集不能为空");
        }
        
        if (superset == null)
        {
            throw new ArgumentNullException(nameof(superset), "超集不能为空");
        }
        
        return subset.IsSubsetOf(superset);
    }

    /// <summary>
    /// 检查一个集合是否是另一个集合的超集
    /// </summary>
    /// <param name="superset">超集</param>
    /// <param name="subset">子集</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>如果是超集则返回true，否则返回false</returns>
    public static bool SetIsSupersetOf<T>(HashSet<T> superset, HashSet<T> subset)
    {
        if (superset == null)
        {
            throw new ArgumentNullException(nameof(superset), "超集不能为空");
        }
        
        if (subset == null)
        {
            throw new ArgumentNullException(nameof(subset), "子集不能为空");
        }
        
        return superset.IsSupersetOf(subset);
    }

    // ========== 通用集合操作 ==========

    /// <summary>
    /// 检查集合是否为空
    /// </summary>
    /// <param name="collection">输入集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>如果为空则返回true，否则返回false</returns>
    public static bool IsEmpty<T>(IEnumerable<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection), "输入集合不能为空");
        }
        
        return !collection.Any();
    }

    /// <summary>
    /// 获取集合的长度
    /// </summary>
    /// <param name="collection">输入集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>集合长度</returns>
    public static int Length<T>(IEnumerable<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection), "输入集合不能为空");
        }
        
        return collection.Count();
    }

    /// <summary>
    /// 将集合转换为数组
    /// </summary>
    /// <param name="collection">输入集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>转换后的数组</returns>
    public static T[] ToArray<T>(IEnumerable<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection), "输入集合不能为空");
        }
        
        return collection.ToArray();
    }

    /// <summary>
    /// 将集合转换为列表
    /// </summary>
    /// <param name="collection">输入集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>转换后的列表</returns>
    public static List<T> ToList<T>(IEnumerable<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection), "输入集合不能为空");
        }
        
        return collection.ToList();
    }

    /// <summary>
    /// 将集合转换为哈希集合
    /// </summary>
    /// <param name="collection">输入集合</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>转换后的哈希集合</returns>
    public static HashSet<T> ToHashSet<T>(IEnumerable<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection), "输入集合不能为空");
        }
        
        return new HashSet<T>(collection);
    }
}