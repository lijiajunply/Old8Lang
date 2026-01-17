using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// ArrayLangValue类型的扩展方法类，提供数组操作功能
/// </summary>
[Serializable]
public static class ArrayValueFuncStatic
{
    /// <param name="arrayValue">数组</param>
    extension(ArrayLangValue arrayValue)
    {
        /// <summary>
        /// 获取数组的长度
        /// </summary>
        /// <returns>数组长度</returns>
        public IntLangValue Count()
        {
            return new IntLangValue(arrayValue.GetLength());
        }

        /// <summary>
        /// 获取数组的长度（Length 属性，与 Count 等效，用于向后兼容和类型一致性）
        /// </summary>
        /// <returns>数组长度</returns>
        public IntLangValue Length()
        {
            return new IntLangValue(arrayValue.GetLength());
        }

        /// <summary>
        /// 对数组进行排序（默认使用快速排序）
        /// </summary>
        /// <returns>排序后的数组（返回新数组）</returns>
        public ArrayLangValue Sort()
        {
            var items = arrayValue.GetItems().ToList();
            var itemsArray = items.ToArray();
            QuickSort(itemsArray, 0, itemsArray.Length - 1);
            return new ArrayLangValue(itemsArray, arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用快速排序算法对数组进行排序
        /// </summary>
        /// <returns>排序后的数组（返回新数组）</returns>
        public ArrayLangValue QuickSort()
        {
            var items = arrayValue.GetItems().ToList();
            var itemsArray = items.ToArray();
            QuickSort(itemsArray, 0, itemsArray.Length - 1);
            return new ArrayLangValue(itemsArray, arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用归并排序算法对数组进行排序
        /// </summary>
        /// <returns>排序后的数组（返回新数组）</returns>
        public ArrayLangValue MergeSort()
        {
            var items = arrayValue.GetItems().ToList();
            var itemsArray = items.ToArray();
            MergeSortImpl(itemsArray, 0, itemsArray.Length - 1);
            return new ArrayLangValue(itemsArray, arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用冒泡排序算法对数组进行排序
        /// </summary>
        /// <returns>排序后的数组（返回新数组）</returns>
        public ArrayLangValue BubbleSort()
        {
            var items = arrayValue.GetItems().ToList();
            var itemsArray = items.ToArray();
            BubbleSortImpl(itemsArray);
            return new ArrayLangValue(itemsArray, arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用选择排序算法对数组进行排序
        /// </summary>
        /// <returns>排序后的数组（返回新数组）</returns>
        public ArrayLangValue SelectionSort()
        {
            var items = arrayValue.GetItems().ToList();
            var itemsArray = items.ToArray();
            SelectionSortImpl(itemsArray);
            return new ArrayLangValue(itemsArray, arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用插入排序算法对数组进行排序
        /// </summary>
        /// <returns>排序后的数组（返回新数组）</returns>
        public ArrayLangValue InsertionSort()
        {
            var items = arrayValue.GetItems().ToList();
            var itemsArray = items.ToArray();
            InsertionSortImpl(itemsArray);
            return new ArrayLangValue(itemsArray, arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用堆排序算法对数组进行排序
        /// </summary>
        /// <returns>排序后的数组（返回新数组）</returns>
        public ArrayLangValue HeapSort()
        {
            var items = arrayValue.GetItems().ToList();
            var itemsArray = items.ToArray();
            HeapSortImpl(itemsArray);
            return new ArrayLangValue(itemsArray, arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 检查数组是否已排序
        /// </summary>
        /// <returns>如果数组已排序返回true，否则返回false</returns>
        public BoolLangValue IsSorted()
        {
            var items = arrayValue.GetItems().ToArray();
            for (int i = 1; i < items.Length; i++)
            {
                if (items[i].Less(items[i - 1]))
                {
                    return new BoolLangValue();
                }
            }
            return new BoolLangValue(true);
        }

        /// <summary>
        /// 移除数组中的重复元素
        /// </summary>
        /// <returns>包含去重后元素的新数组</returns>
        public ArrayLangValue Distinct()
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

            return new ArrayLangValue(distinct.ToArray(), arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用转换函数映射数组元素
        /// </summary>
        /// <param name="transform">转换函数，将元素转换为新值</param>
        /// <returns>包含转换后元素的新数组</returns>
        public ArrayLangValue Map(FuncLangValue transform)
        {
            var mapped = new List<LangValueType>();
            var items = arrayValue.GetItems();

            foreach (var item in items)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();

                // 执行转换函数，传递当前元素作为参数
                var result = transform.Run(manager, [item]);
                mapped.Add(result);
            }

            return new ArrayLangValue(mapped.ToArray(), arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用谓词函数过滤数组元素
        /// </summary>
        /// <param name="predicate">谓词函数，返回布尔值</param>
        /// <returns>包含满足条件元素的新数组</returns>
        public ArrayLangValue Filter(FuncLangValue predicate)
        {
            var filtered = new List<LangValueType>();
            var items = arrayValue.GetItems();

            foreach (var item in items)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();

                // 执行谓词函数，传递当前元素作为参数
                var result = predicate.Run(manager, [item]);

                // 如果结果为真，则保留该元素
                if (result is BoolLangValue { Value: true })
                {
                    filtered.Add(item);
                }
            }

            return new ArrayLangValue(filtered.ToArray(), arrayValue.ElementType, arrayValue.Position);
        }

        /// <summary>
        /// 使用归约函数将数组元素归约为单个值
        /// </summary>
        /// <param name="reducer">归约函数，接受累加器和当前元素，返回新的累加器值</param>
        /// <param name="initialValue">初始累加器值</param>
        /// <returns>归约后的结果值</returns>
        public LangValueType Reduce(FuncLangValue reducer,
            LangValueType initialValue)
        {
            var accumulator = initialValue;
            var items = arrayValue.GetItems();

            foreach (var item in items)
            {
                // 创建临时变量管理器
                var manager = new VariateManager();

                // 执行归约函数，传递累加器和当前元素作为参数
                accumulator = reducer.Run(manager, [accumulator, item]);
            }

            return accumulator;
        }

        public ListLangValue ToList()
        {
            return new ListLangValue(arrayValue.GetItems().ToList());
        }
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
    /// 归并排序的递归实现
    /// </summary>
    /// <param name="nums">要排序的数组</param>
    /// <param name="left">排序范围的左边界</param>
    /// <param name="right">排序范围的右边界</param>
    private static void MergeSortImpl(LangValueType[] nums, int left, int right)
    {
        if (left < right)
        {
            int mid = left + (right - left) / 2;
            MergeSortImpl(nums, left, mid);
            MergeSortImpl(nums, mid + 1, right);
            Merge(nums, left, mid, right);
        }
    }

    /// <summary>
    /// 归并排序的合并操作
    /// </summary>
    /// <param name="nums">要合并的数组</param>
    /// <param name="left">左子数组的起始索引</param>
    /// <param name="mid">左子数组的结束索引</param>
    /// <param name="right">右子数组的结束索引</param>
    private static void Merge(LangValueType[] nums, int left, int mid, int right)
    {
        int n1 = mid - left + 1;
        int n2 = right - mid;

        var leftArray = new LangValueType[n1];
        var rightArray = new LangValueType[n2];

        Array.Copy(nums, left, leftArray, 0, n1);
        Array.Copy(nums, mid + 1, rightArray, 0, n2);

        int i = 0, j = 0, k = left;

        while (i < n1 && j < n2)
        {
            if (!rightArray[j].Less(leftArray[i]))
            {
                nums[k] = leftArray[i];
                i++;
            }
            else
            {
                nums[k] = rightArray[j];
                j++;
            }
            k++;
        }

        while (i < n1)
        {
            nums[k] = leftArray[i];
            i++;
            k++;
        }

        while (j < n2)
        {
            nums[k] = rightArray[j];
            j++;
            k++;
        }
    }

    /// <summary>
    /// 冒泡排序实现
    /// </summary>
    /// <param name="nums">要排序的数组</param>
    private static void BubbleSortImpl(LangValueType[] nums)
    {
        int n = nums.Length;
        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - i - 1; j++)
            {
                if (nums[j + 1].Less(nums[j]))
                {
                    Swap(nums, j, j + 1);
                    swapped = true;
                }
            }
            // 如果这一轮没有发生交换，说明数组已排序
            if (!swapped) break;
        }
    }

    /// <summary>
    /// 选择排序实现
    /// </summary>
    /// <param name="nums">要排序的数组</param>
    private static void SelectionSortImpl(LangValueType[] nums)
    {
        int n = nums.Length;
        for (int i = 0; i < n - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < n; j++)
            {
                if (nums[j].Less(nums[minIndex]))
                {
                    minIndex = j;
                }
            }
            if (minIndex != i)
            {
                Swap(nums, i, minIndex);
            }
        }
    }

    /// <summary>
    /// 插入排序实现
    /// </summary>
    /// <param name="nums">要排序的数组</param>
    private static void InsertionSortImpl(LangValueType[] nums)
    {
        int n = nums.Length;
        for (int i = 1; i < n; i++)
        {
            var key = nums[i];
            int j = i - 1;

            while (j >= 0 && key.Less(nums[j]))
            {
                nums[j + 1] = nums[j];
                j--;
            }
            nums[j + 1] = key;
        }
    }

    /// <summary>
    /// 堆排序实现
    /// </summary>
    /// <param name="nums">要排序的数组</param>
    private static void HeapSortImpl(LangValueType[] nums)
    {
        int n = nums.Length;

        // 构建最大堆
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(nums, n, i);
        }

        // 逐个提取元素
        for (int i = n - 1; i > 0; i--)
        {
            Swap(nums, 0, i);
            Heapify(nums, i, 0);
        }
    }

    /// <summary>
    /// 堆排序的堆化操作
    /// </summary>
    /// <param name="nums">要堆化的数组</param>
    /// <param name="n">堆的大小</param>
    /// <param name="i">当前需要堆化的节点索引</param>
    private static void Heapify(LangValueType[] nums, int n, int i)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < n && nums[largest].Less(nums[left]))
        {
            largest = left;
        }

        if (right < n && nums[largest].Less(nums[right]))
        {
            largest = right;
        }

        if (largest != i)
        {
            Swap(nums, i, largest);
            Heapify(nums, n, largest);
        }
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