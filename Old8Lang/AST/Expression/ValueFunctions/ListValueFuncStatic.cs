using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// ListLangValue类型的扩展方法类，提供列表操作功能
/// </summary>
[Serializable]
public static class ListValueFuncStatic
{
    /// <param name="langValue">字符串数组</param>
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
        /// 返回列表元素数量
        /// </summary>
        /// <returns>包含元素数量的IntLangValue</returns>
        public IntLangValue Count()
        {
            return new IntLangValue(langValue.Values.Count);
        }

        /// <summary>
        /// 对列表进行排序
        /// </summary>
        /// <returns>排序后的新列表</returns>
        public ListLangValue Sort()
        {
            // 创建新列表副本以避免修改原列表
            var sortedValues = new List<LangValueType>(langValue.Values);
            QuickSort(sortedValues, 0, sortedValues.Count - 1);
            return new ListLangValue(sortedValues);
        }

        /// <summary>
        /// 使用自定义比较函数对列表进行排序
        /// </summary>
        /// <param name="comparer">比较函数，接受两个元素，返回负数（a<b）、0（a==b）或正数（a>b）</param>
        /// <returns>排序后的新列表</returns>
        public ListLangValue Sort(FuncLangValue comparer)
        {
            // 创建新列表副本以避免修改原列表
            var sortedValues = new List<LangValueType>(langValue.Values);
            QuickSortWithComparer(sortedValues, 0, sortedValues.Count - 1, comparer);
            return new ListLangValue(sortedValues);
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

                // 执行谓词函数
                var result = predicate.Run(manager, new List<LangExpression> { item });

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

                // 执行转换函数
                var result = transform.Run(manager, new List<LangExpression> { item });
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

                // 执行归约函数
                accumulator = reducer.Run(manager, new List<LangExpression> { accumulator, item });
            }

            return accumulator;
        }

        /// <summary>
        /// 反转列表元素顺序
        /// </summary>
        /// <returns>反转后的新列表</returns>
        public ListLangValue Reverse()
        {
            // 创建新列表副本以避免修改原列表
            var reversedValues = new List<LangValueType>(langValue.Values);
            reversedValues.Reverse();
            return new ListLangValue(reversedValues);
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
        /// 连接两个列表，返回包含所有元素的新列表
        /// </summary>
        /// <param name="otherList">要连接的另一个列表</param>
        /// <returns>包含两个列表所有元素的新列表</returns>
        public ListLangValue Concat(ListLangValue otherList)
        {
            var result = new List<LangValueType>(langValue.Values);
            result.AddRange(otherList.Values);
            return new ListLangValue(result);
        }

        /// <summary>
        /// 对列表元素进行聚合操作
        /// </summary>
        /// <param name="accumulator">聚合函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <returns>聚合后的结果值</returns>
        public LangValueType Aggregate(FuncLangValue accumulator)
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法对空列表进行聚合操作");
            }

            var result = langValue.Values[0];
            for (var i = 1; i < langValue.Values.Count; i++)
            {
                var manager = new VariateManager();

                result = accumulator.Run(manager, new List<LangExpression> { result, langValue.Values[i] });
            }

            return result;
        }

        /// <summary>
        /// 对列表元素进行聚合操作，使用指定的初始值
        /// </summary>
        /// <param name="accumulator">聚合函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <param name="seed">聚合的初始值</param>
        /// <returns>聚合后的结果值</returns>
        public LangValueType Aggregate(FuncLangValue accumulator, LangValueType seed)
        {
            var result = seed;
            for (var i = 0; i < langValue.Values.Count; i++)
            {
                var manager = new VariateManager();

                result = accumulator.Run(manager, new List<LangExpression> { result, langValue.Values[i] });
            }

            return result;
        }

        /// <summary>
        /// 查找列表中第一个满足条件的元素
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>第一个满足条件的元素，如果找不到则返回Null</returns>
        public LangValueType Find(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();

                var result = predicate.Run(manager, new List<LangExpression> { item });
                if (result is BoolLangValue { Value: true })
                {
                    return item;
                }
            }

            return new NullLangValue();
        }

        /// <summary>
        /// 跳过列表的前n个元素，返回剩余元素
        /// </summary>
        /// <param name="count">要跳过的元素数量</param>
        /// <returns>包含剩余元素的新列表</returns>
        public ListLangValue Skip(IntLangValue count)
        {
            var skipCount = Math.Max(0, Math.Min(count.Value, langValue.Values.Count));
            var result = langValue.Values.Skip(skipCount).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 检查列表中是否有任何元素满足条件
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>如果有元素满足条件则返回true，否则返回false</returns>
        public BoolLangValue Any(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();

                var result = predicate.Run(manager, new List<LangExpression> { item });
                if (result is BoolLangValue { Value: true })
                {
                    return new BoolLangValue(true);
                }
            }

            return new BoolLangValue(false);
        }

        /// <summary>
        /// 在指定索引处插入元素
        /// </summary>
        /// <param name="index">插入位置的索引</param>
        /// <param name="element">要插入的元素</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue Insert(IntLangValue index, LangValueType element)
        {
            var insertIndex = Math.Max(0, Math.Min(index.Value, langValue.Values.Count));
            langValue.Values.Insert(insertIndex, element);
            return new VoidLangValue();
        }

        /// <summary>
        /// 清空列表中的所有元素
        /// </summary>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue Clear()
        {
            langValue.ClearInternal();
            return new VoidLangValue();
        }

        /// <summary>
        /// 对列表中的每个元素执行操作
        /// </summary>
        /// <param name="action">操作函数，接受当前元素作为参数</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue ForEach(FuncLangValue action)
        {
            // 获取当前的 VariateManager
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                // 如果没有找到外部 manager，创建新的
                manager = new VariateManager();
            }

            // 检查 action 是否是 lambda（lambda 通常 Id 为 null 且没有 Method）
            bool isLambda = action.Id == null && action.Method == null;

            if (isLambda)
            {
                // 对于 lambda，我们需要避免使用闭包机制（深拷贝）
                // 而是直接传递原始 manager 以支持外部变量访问
                foreach (var item in langValue.Values)
                {
                    // 直接执行 lambda 的主体，不创建闭包
                    // 保存当前作用域
                    var savedScopes = new List<Dictionary<string, LangValueType>>(manager.Scopes);

                    try
                    {
                        // 添加新的作用域层级
                        manager.AddChildren();
                        manager.IsFunc = true;

                        // 将参数添加到当前作用域
                        if (action.Ids.Count == 1)
                        {
                            var paramId = action.Ids[0];
                            manager.Set(paramId, item);
                        }
                        else if (action.Ids.Count > 0)
                        {
                            // 如果有多个参数，将 item 作为第一个参数
                            var firstParamId = action.Ids[0];
                            manager.Set(firstParamId, item);
                        }

                        // 执行 lambda 主体
                        action.BlockStatement.Run(manager);
                    }
                    finally
                    {
                        // 恢复作用域
                        manager.Scopes.Clear();
                        manager.Scopes.AddRange(savedScopes);
                        manager.IsFunc = false;
                        manager.IsReturn = false;
                        manager.Result = null;
                    }
                }
            }
            else
            {
                // 对于非 lambda（原生方法），使用正常的 Run 调用
                foreach (var item in langValue.Values)
                {
                    action.Run(manager, new List<LangExpression> { item });
                }
            }

            return new VoidLangValue();
        }

        /// <summary>
        /// 将列表转换为数组
        /// </summary>
        /// <returns>包含相同元素的新数组</returns>
        public ArrayLangValue ToArray()
        {
            return new ArrayLangValue(langValue.Values);
        }


        /// <summary>
        /// 检查列表中是否所有元素都满足条件
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>如果所有元素都满足条件则返回true，否则返回false</returns>
        public BoolLangValue All(FuncLangValue predicate)
        {
            foreach (var item in langValue.Values)
            {
                var manager = new VariateManager();

                var result = predicate.Run(manager, new List<LangExpression> { item });
                if (result is BoolLangValue { Value: false })
                {
                    return new BoolLangValue(false);
                }
            }

            return new BoolLangValue(true);
        }

        /// <summary>
        /// 获取列表的前n个元素
        /// </summary>
        /// <param name="count">要获取的元素数量</param>
        /// <returns>包含前n个元素的新列表</returns>
        public ListLangValue Take(IntLangValue count)
        {
            var takeCount = Math.Max(0, Math.Min(count.Value, langValue.Values.Count));
            var result = langValue.Values.Take(takeCount).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 移除并返回列表的最后一个元素
        /// </summary>
        /// <returns>被移除的最后一个元素</returns>
        /// <exception cref="InvalidOperationError">当列表为空时抛出</exception>
        public LangValueType Pop()
        {
            if (langValue.Values.Count == 0)
            {
                throw new InvalidOperationError(langValue, "无法从空列表中移除元素");
            }

            var lastIndex = langValue.Values.Count - 1;
            var lastElement = langValue.Values[lastIndex];
            langValue.Values.RemoveAt(lastIndex);
            return lastElement;
        }

        /// <summary>
        /// 获取列表的切片（子列表）
        /// </summary>
        /// <param name="start">起始索引（包含）</param>
        /// <param name="end">结束索引（不包含）</param>
        /// <returns>包含切片元素的新列表</returns>
        public ListLangValue Slice(IntLangValue start, IntLangValue end)
        {
            var startIndex = Math.Max(0, Math.Min(start.Value, langValue.Values.Count));
            var endIndex = Math.Max(0, Math.Min(end.Value, langValue.Values.Count));

            if (startIndex > endIndex)
            {
                return new ListLangValue(new List<LangValueType>());
            }

            var result = langValue.Values.Skip(startIndex).Take(endIndex - startIndex).ToList();
            return new ListLangValue(result);
        }

        /// <summary>
        /// 连接字符串数组为单个字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>连接后的字符串</returns>
        public StringLangValue Join(StringLangValue separator)
        {
            if (langValue == null)
            {
                throw new ArgumentNullException(nameof(langValue), "字符串数组不能为空");
            }

            return new StringLangValue(string.Join(separator.Value, langValue.Values.Select(v => v.ToDisplayString())));
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
    /// 使用自定义比较器的快速排序算法
    /// </summary>
    /// <param name="nums">要排序的列表</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    /// <param name="comparer">比较函数</param>
    private static void QuickSortWithComparer(List<LangValueType> nums, int left, int right, FuncLangValue comparer)
    {
        while (true)
        {
            if (left < right)
            {
                int pivotIndex = PartitionWithComparer(nums, left, right, comparer);
                QuickSortWithComparer(nums, left, pivotIndex - 1, comparer);
                left = pivotIndex + 1;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// 使用自定义比较器的快速排序分区函数
    /// </summary>
    /// <param name="nums">要分区的列表</param>
    /// <param name="left">分区范围的左边界</param>
    /// <param name="right">分区范围的右边界</param>
    /// <param name="comparer">比较函数</param>
    /// <returns>枢轴元素的最终位置</returns>
    private static int PartitionWithComparer(List<LangValueType> nums, int left, int right, FuncLangValue comparer)
    {
        var pivot = nums[right];
        var i = left - 1;
        var manager = new VariateManager();

        for (var j = left; j < right; j++)
        {
            // 调用比较函数：comparer(nums[j], pivot)
            // 如果返回负数，表示 nums[j] < pivot
            var result = comparer.Run(manager, new List<LangExpression> { nums[j], pivot });
            if (result is IntLangValue intResult && intResult.Value < 0)
            {
                i++;
                Swap(nums, i, j);
            }
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