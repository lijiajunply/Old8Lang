using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// ArrayLangValue类型的扩展方法类，提供数组操作功能
/// </summary>
[Serializable]
public static class ArrayValueFuncStatic
{
    /// <summary>
    /// 获取数组的长度
    /// </summary>
    /// <param name="arrayValue">数组</param>
    /// <returns>数组长度</returns>
    public static IntLangValue Count(this ArrayLangValue arrayValue)
    {
        return new IntLangValue(arrayValue.GetLength());
    }

    /// <summary>
    /// 对数组进行排序
    /// </summary>
    /// <param name="arrayValue">要排序的数组</param>
    /// <returns>排序后的数组（返回新数组）</returns>
    public static ArrayLangValue Sort(this ArrayLangValue arrayValue)
    {
        var items = arrayValue.GetItems().ToList();
        var itemsArray = items.ToArray();
        QuickSort(itemsArray, 0, itemsArray.Length - 1);
        return new ArrayLangValue(itemsArray.ToList());
    }

    /// <summary>
    /// 移除数组中的重复元素
    /// </summary>
    /// <param name="arrayValue">要去重的数组</param>
    /// <returns>包含去重后元素的新数组</returns>
    public static ArrayLangValue Distinct(this ArrayLangValue arrayValue)
    {
        var distinct = new List<LangValueType>();
        var items = arrayValue.GetItems();

        foreach (var item in items)
        {
            var isDuplicate = false;
            foreach (var d in distinct)
            {
                if (item.Equal(d))
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                distinct.Add(item);
            }
        }

        return new ArrayLangValue(distinct);
    }

    /// <summary>
    /// 使用转换函数映射数组元素
    /// </summary>
    /// <param name="arrayValue">要映射的数组</param>
    /// <param name="transform">转换函数，将元素转换为新值</param>
    /// <returns>包含转换后元素的新数组</returns>
    public static ArrayLangValue Map(this ArrayLangValue arrayValue, FuncLangValue transform)
    {
        var mapped = new List<LangValueType>();
        var items = arrayValue.GetItems();

        foreach (var item in items)
        {
            // 创建临时变量管理器
            var manager = new VariateManager();

            // 执行转换函数，传递当前元素作为参数
            var result = transform.Run(manager, new List<LangExpression> { item });
            mapped.Add(result);
        }

        return new ArrayLangValue(mapped);
    }

    /// <summary>
    /// 使用谓词函数过滤数组元素
    /// </summary>
    /// <param name="arrayValue">要过滤的数组</param>
    /// <param name="predicate">谓词函数，返回布尔值</param>
    /// <returns>包含满足条件元素的新数组</returns>
    public static ArrayLangValue Filter(this ArrayLangValue arrayValue, FuncLangValue predicate)
    {
        var filtered = new List<LangValueType>();
        var items = arrayValue.GetItems();

        foreach (var item in items)
        {
            // 创建临时变量管理器
            var manager = new VariateManager();

            // 执行谓词函数，传递当前元素作为参数
            var result = predicate.Run(manager, new List<LangExpression> { item });

            // 如果结果为真，则保留该元素
            if (result is BoolLangValue { Value: true })
            {
                filtered.Add(item);
            }
        }

        return new ArrayLangValue(filtered);
    }

    /// <summary>
    /// 使用归约函数将数组元素归约为单个值
    /// </summary>
    /// <param name="arrayValue">要归约的数组</param>
    /// <param name="reducer">归约函数，接受累加器和当前元素，返回新的累加器值</param>
    /// <param name="initialValue">初始累加器值</param>
    /// <returns>归约后的结果值</returns>
    public static LangValueType Reduce(this ArrayLangValue arrayValue, FuncLangValue reducer, LangValueType initialValue)
    {
        var accumulator = initialValue;
        var items = arrayValue.GetItems();

        foreach (var item in items)
        {
            // 创建临时变量管理器
            var manager = new VariateManager();

            // 执行归约函数，传递累加器和当前元素作为参数
            accumulator = reducer.Run(manager, new List<LangExpression> { accumulator, item });
        }

        return accumulator;
    }

    /// <summary>
    /// 使用快速排序算法对数组进行排序
    /// </summary>
    /// <param name="nums">要排序的数组</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    private static void QuickSort(LangValueType[] nums, int left, int right)
    {
        while (true)
        {
            if (left < right)
            {
                int pivotIndex = Partition(nums, left, right);
                QuickSort(nums, left, pivotIndex - 1);
                left = pivotIndex + 1;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// 快速排序的分区函数，选择最右侧元素作为枢轴
    /// </summary>
    /// <param name="nums">要分区的数组</param>
    /// <param name="left">分区范围的左边界</param>
    /// <param name="right">分区范围的右边界</param>
    /// <returns>枢轴元素的最终位置</returns>
    private static int Partition(LangValueType[] nums, int left, int right)
    {
        var pivot = nums[right];
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (!nums[j].Less(pivot)) continue;
            i++;
            Swap(nums, i, j);
        }

        Swap(nums, i + 1, right);
        return i + 1;
    }

    /// <summary>
    /// 交换数组中两个元素的位置
    /// </summary>
    /// <param name="nums">数组</param>
    /// <param name="i">第一个元素的索引</param>
    /// <param name="j">第二个元素的索引</param>
    private static void Swap(LangValueType[] nums, int i, int j)
    {
        (nums[i], nums[j]) = (nums[j], nums[i]);
    }
}