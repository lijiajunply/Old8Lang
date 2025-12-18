using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// ListLangValue类型的扩展方法类，提供列表操作功能
/// </summary>
[Serializable]
public static class ListValueFuncStatic
{
    /// <summary>
    /// 连接字符串数组为单个字符串
    /// </summary>
    /// <param name="langValue">字符串数组</param>
    /// <param name="separator">分隔符</param>
    /// <returns>连接后的字符串</returns>
    public static StringLangValue Join(this ListLangValue langValue, StringLangValue separator)
    {
        if (langValue == null)
        {
            throw new ArgumentNullException(nameof(langValue), "字符串数组不能为空");
        }

        return new StringLangValue(string.Join(separator.Value, langValue));
    }

    extension(ListLangValue langValue)
    {
        /// <summary>
        /// 向列表中添加元素
        /// </summary>
        /// <param name="langValueType">要添加的元素</param>
        /// <returns>添加的元素</returns>
        public LangValueType Add(LangValueType langValueType)
        {
            langValue.Values.Add(langValueType);
            return langValueType;
        }

        /// <summary>
        /// 从列表中移除指定元素
        /// </summary>
        /// <param name="num">要移除的元素</param>
        /// <returns>被移除的元素</returns>
        /// <exception cref="InvalidOperationError">当元素不存在时抛出</exception>
        public LangValueType Remove(LangValueType num)
        {
            for (var i = 0; i < langValue.Values.Count; i++)
            {
                if (!langValue.Values[i].Equal(num)) continue;
                var a = langValue.Values[i];
                langValue.Values.RemoveAt(i);
                return a;
            }

            throw new InvalidOperationError(langValue, "找不到要移除的元素");
        }

        /// <summary>
        /// 根据索引从列表中移除元素
        /// </summary>
        /// <param name="num">要移除的元素索引</param>
        /// <returns>被移除的元素</returns>
        public LangValueType RemoveAt(IntLangValue num)
        {
            var a = langValue.Values[num.Value];
            langValue.Values.RemoveAt(num.Value);
            return a;
        }

        /// <summary>
        /// 将另一个列表的所有元素添加到当前列表
        /// </summary>
        /// <param name="otherLangValue">要添加的列表</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue AddList(ListLangValue otherLangValue)
        {
            langValue.Values.AddRange(otherLangValue.Values);
            return new VoidLangValue();
        }

        /// <summary>
        /// 对列表进行排序
        /// </summary>
        /// <returns>排序后的列表（原地排序）</returns>
        public ListLangValue Sort()
        {
            QuickSort(langValue.Values, 0, langValue.Values.Count - 1);
            return langValue;
        }

        /// <summary>
        /// 使用谓词函数过滤列表元素
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>包含满足条件元素的新列表</returns>
        public ListLangValue Filter(FuncLangValue predicate)
        {
            var filtered = new List<LangValueType>();
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();
                manager.Set(new LangId("item"), item);

                // 执行谓词函数
                var result = predicate.Run(manager);

                // 如果结果为真，则保留该元素
                if (result is BoolLangValue { Value: true })
                {
                    filtered.Add(item);
                }
            }

            return new ListLangValue(filtered);
        }

        /// <summary>
        /// 使用转换函数映射列表元素
        /// </summary>
        /// <param name="transform">转换函数，将元素转换为新值</param>
        /// <returns>包含转换后元素的新列表</returns>
        public ListLangValue Map(FuncLangValue transform)
        {
            var mapped = new List<LangValueType>();
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();
                manager.Set(new LangId("item"), item);

                // 执行转换函数
                var result = transform.Run(manager);
                mapped.Add(result);
            }

            return new ListLangValue(mapped);
        }

        /// <summary>
        /// 使用归约函数将列表元素归约为单个值
        /// </summary>
        /// <param name="reducer">归约函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <param name="initialValue">初始累加器值</param>
        /// <returns>归约后的结果值</returns>
        public LangValueType Reduce(FuncLangValue reducer, LangValueType initialValue)
        {
            var accumulator = initialValue;
            foreach (var item in langValue.Values)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();
                manager.Set(new LangId("accumulator"), accumulator);
                manager.Set(new LangId("item"), item);

                // 执行归约函数
                accumulator = reducer.Run(manager);
            }

            return accumulator;
        }

        /// <summary>
        /// 反转列表元素顺序
        /// </summary>
        /// <returns>反转后的列表（原地反转）</returns>
        public ListLangValue Reverse()
        {
            langValue.Values.Reverse();
            return langValue;
        }

        /// <summary>
        /// 检查列表是否包含指定元素
        /// </summary>
        /// <param name="element">要检查的元素</param>
        /// <returns>包含检查结果的BoolLangValue</returns>
        public BoolLangValue Contains(LangValueType element)
        {
            return new BoolLangValue(langValue.Values.Any(item => item.Equal(element)));
        }

        /// <summary>
        /// 查找元素在列表中第一次出现的索引
        /// </summary>
        /// <param name="element">要查找的元素</param>
        /// <returns>包含索引的IntLangValue，未找到返回-1</returns>
        public IntLangValue IndexOf(LangValueType element)
        {
            for (var i = 0; i < langValue.Values.Count; i++)
            {
                if (langValue.Values[i].Equal(element))
                {
                    return new IntLangValue(i);
                }
            }

            return new IntLangValue(-1); // 未找到返回-1
        }

        /// <summary>
        /// 获取列表元素数量
        /// </summary>
        /// <returns>包含元素数量的IntLangValue</returns>
        public IntLangValue Count()
        {
            return new IntLangValue(langValue.Values.Count);
        }
    }

    /// <summary>
    /// 使用快速排序算法对列表进行排序
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    private static void QuickSort(List<LangValueType> nums, int left, int right)
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
    /// <param name="nums">要分区的列表</param>
    /// <param name="left">分区范围的左边界</param>
    /// <param name="right">分区范围的右边界</param>
    /// <returns>枢轴元素的最终位置</returns>
    private static int Partition(List<LangValueType> nums, int left, int right)
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
    /// 交换列表中两个元素的位置
    /// </summary>
    /// <param name="nums">列表</param>
    /// <param name="i">第一个元素的索引</param>
    /// <param name="j">第二个元素的索引</param>
    private static void Swap(List<LangValueType> nums, int i, int j)
    {
        (nums[i], nums[j]) = (nums[j], nums[i]);
    }
}