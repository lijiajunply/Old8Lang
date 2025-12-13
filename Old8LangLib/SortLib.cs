namespace Old8LangLib;

/// <summary>
/// 排序算法模块，实现各种排序算法
/// </summary>
public static class SortLib
{
    // ========== 快速排序 ==========

    /// <summary>
    /// 快速排序算法
    /// </summary>
    /// <param name="array">输入数组</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的数组</returns>
    public static T[] QuickSort<T>(T[] array) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "输入数组不能为空");
        }

        var result = new T[array.Length];
        Array.Copy(array, result, array.Length);
        QuickSortImpl(result, 0, result.Length - 1);
        return result;
    }

    /// <summary>
    /// 快速排序实现
    /// </summary>
    private static void QuickSortImpl<T>(T[] array, int low, int high) where T : IComparable<T>
    {
        if (low < high)
        {
            int pivotIndex = Partition(array, low, high);
            QuickSortImpl(array, low, pivotIndex - 1);
            QuickSortImpl(array, pivotIndex + 1, high);
        }
    }

    /// <summary>
    /// 分区操作
    /// </summary>
    private static int Partition<T>(T[] array, int low, int high) where T : IComparable<T>
    {
        T pivot = array[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (array[j].CompareTo(pivot) <= 0)
            {
                i++;
                Swap(array, i, j);
            }
        }

        Swap(array, i + 1, high);
        return i + 1;
    }

    // ========== 归并排序 ==========

    /// <summary>
    /// 归并排序算法
    /// </summary>
    /// <param name="array">输入数组</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的数组</returns>
    public static T[] MergeSort<T>(T[] array) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "输入数组不能为空");
        }

        if (array.Length <= 1)
        {
            return array;
        }

        var result = new T[array.Length];
        Array.Copy(array, result, array.Length);
        MergeSortImpl(result, 0, result.Length - 1);
        return result;
    }

    /// <summary>
    /// 归并排序实现
    /// </summary>
    private static void MergeSortImpl<T>(T[] array, int left, int right) where T : IComparable<T>
    {
        if (left < right)
        {
            int mid = left + (right - left) / 2;
            MergeSortImpl(array, left, mid);
            MergeSortImpl(array, mid + 1, right);
            Merge(array, left, mid, right);
        }
    }

    /// <summary>
    /// 合并操作
    /// </summary>
    private static void Merge<T>(T[] array, int left, int mid, int right) where T : IComparable<T>
    {
        int n1 = mid - left + 1;
        int n2 = right - mid;

        T[] leftArray = new T[n1];
        T[] rightArray = new T[n2];

        Array.Copy(array, left, leftArray, 0, n1);
        Array.Copy(array, mid + 1, rightArray, 0, n2);

        int i = 0, j = 0, k = left;

        while (i < n1 && j < n2)
        {
            if (leftArray[i].CompareTo(rightArray[j]) <= 0)
            {
                array[k] = leftArray[i];
                i++;
            }
            else
            {
                array[k] = rightArray[j];
                j++;
            }

            k++;
        }

        while (i < n1)
        {
            array[k] = leftArray[i];
            i++;
            k++;
        }

        while (j < n2)
        {
            array[k] = rightArray[j];
            j++;
            k++;
        }
    }

    // ========== 冒泡排序 ==========

    /// <summary>
    /// 冒泡排序算法
    /// </summary>
    /// <param name="array">输入数组</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的数组</returns>
    public static T[] BubbleSort<T>(T[] array) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "输入数组不能为空");
        }

        var result = new T[array.Length];
        Array.Copy(array, result, array.Length);

        for (int i = 0; i < result.Length - 1; i++)
        {
            for (int j = 0; j < result.Length - i - 1; j++)
            {
                if (result[j].CompareTo(result[j + 1]) > 0)
                {
                    Swap(result, j, j + 1);
                }
            }
        }

        return result;
    }

    // ========== 选择排序 ==========

    /// <summary>
    /// 选择排序算法
    /// </summary>
    /// <param name="array">输入数组</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的数组</returns>
    public static T[] SelectionSort<T>(T[] array) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "输入数组不能为空");
        }

        var result = new T[array.Length];
        Array.Copy(array, result, array.Length);

        for (int i = 0; i < result.Length - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < result.Length; j++)
            {
                if (result[j].CompareTo(result[minIndex]) < 0)
                {
                    minIndex = j;
                }
            }

            Swap(result, i, minIndex);
        }

        return result;
    }

    // ========== 插入排序 ==========

    /// <summary>
    /// 插入排序算法
    /// </summary>
    /// <param name="array">输入数组</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的数组</returns>
    public static T[] InsertionSort<T>(T[] array) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "输入数组不能为空");
        }

        var result = new T[array.Length];
        Array.Copy(array, result, array.Length);

        for (int i = 1; i < result.Length; i++)
        {
            T key = result[i];
            int j = i - 1;

            while (j >= 0 && result[j].CompareTo(key) > 0)
            {
                result[j + 1] = result[j];
                j--;
            }

            result[j + 1] = key;
        }

        return result;
    }

    // ========== 堆排序 ==========

    /// <summary>
    /// 堆排序算法
    /// </summary>
    /// <param name="array">输入数组</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的数组</returns>
    public static T[] HeapSort<T>(T[] array) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "输入数组不能为空");
        }

        var result = new T[array.Length];
        Array.Copy(array, result, array.Length);

        int n = result.Length;

        // 构建最大堆
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(result, n, i);
        }

        // 逐个提取元素
        for (int i = n - 1; i > 0; i--)
        {
            Swap(result, 0, i);
            Heapify(result, i, 0);
        }

        return result;
    }

    /// <summary>
    /// 堆化操作
    /// </summary>
    private static void Heapify<T>(T[] array, int n, int i) where T : IComparable<T>
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < n && array[left].CompareTo(array[largest]) > 0)
        {
            largest = left;
        }

        if (right < n && array[right].CompareTo(array[largest]) > 0)
        {
            largest = right;
        }

        if (largest != i)
        {
            Swap(array, i, largest);
            Heapify(array, n, largest);
        }
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 交换数组中的两个元素
    /// </summary>
    private static void Swap<T>(T[] array, int i, int j)
    {
        (array[i], array[j]) = (array[j], array[i]);
    }

    /// <summary>
    /// 检查数组是否已排序
    /// </summary>
    /// <param name="array">输入数组</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>如果已排序则返回true，否则返回false</returns>
    public static bool IsSorted<T>(T[] array) where T : IComparable<T>
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "输入数组不能为空");
        }

        for (int i = 1; i < array.Length; i++)
        {
            if (array[i - 1].CompareTo(array[i]) > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 对列表进行排序
    /// </summary>
    /// <param name="list">输入列表</param>
    /// <param name="sortType">排序算法类型，可选值：quick, merge, bubble, selection, insertion, heap</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>排序后的列表</returns>
    public static List<T> SortList<T>(List<T> list, string sortType = "quick") where T : IComparable<T>
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "输入列表不能为空");
        }

        T[] array = list.ToArray();
        T[] sortedArray;

        switch (sortType.ToLower())
        {
            case "quick":
                sortedArray = QuickSort(array);
                break;
            case "merge":
                sortedArray = MergeSort(array);
                break;
            case "bubble":
                sortedArray = BubbleSort(array);
                break;
            case "selection":
                sortedArray = SelectionSort(array);
                break;
            case "insertion":
                sortedArray = InsertionSort(array);
                break;
            case "heap":
                sortedArray = HeapSort(array);
                break;
            default:
                throw new ArgumentException($"不支持的排序算法类型: {sortType}", nameof(sortType));
        }

        return sortedArray.ToList();
    }
}