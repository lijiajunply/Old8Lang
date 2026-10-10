using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.QuickSort() - 使用快速排序算法对数组进行排序
/// </summary>
public class ArrayQuickSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["QuickSort", "quickSort"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var items = array.GetItems().ToArray();
        QuickSort(items, 0, items.Length - 1);
        return new ArrayLangValue(items, array.ElementType, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(ArrayQuickSortMethod).GetMethod(nameof(QuickSortHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ArrayLangValue QuickSortHelper(ArrayLangValue array)
    {
        var items = array.GetItems().ToArray();
        QuickSort(items, 0, items.Length - 1);
        return new ArrayLangValue(items, array.ElementType, array.Position);
    }

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

    private static int Partition(LangValueType[] nums, int left, int right)
    {
        var pivot = nums[right];
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (!nums[j].Less(pivot)) continue;
            i++;
            (nums[i], nums[j]) = (nums[j], nums[i]);
        }

        (nums[i + 1], nums[right]) = (nums[right], nums[i + 1]);
        return i + 1;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ArrayLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return ArraySortVmSupport.SortAndReturn(instance, QuickSort);
    }

    /// <summary>
    /// VM 快速排序：Lomuto 分区 + 三数取中 + 只递归较小一侧（递归深度 O(log n)）。
    /// </summary>
    private static void QuickSort(List<object?> items) => QuickSortRange(items, 0, items.Count - 1);

    private static void QuickSortRange(List<object?> items, int left, int right)
    {
        while (left < right)
        {
            var pivotIndex = Partition(items, left, right);

            if (pivotIndex - left < right - pivotIndex)
            {
                QuickSortRange(items, left, pivotIndex - 1);
                left = pivotIndex + 1;
            }
            else
            {
                QuickSortRange(items, pivotIndex + 1, right);
                right = pivotIndex - 1;
            }
        }
    }

    private static int Partition(List<object?> items, int left, int right)
    {
        // 三数取中，避免已排序 / 逆序输入退化成 O(n^2)
        var mid = left + (right - left) / 2;
        if (ArraySortVmSupport.Less(items[mid], items[left])) ArraySortVmSupport.Swap(items, mid, left);
        if (ArraySortVmSupport.Less(items[right], items[left])) ArraySortVmSupport.Swap(items, right, left);
        if (ArraySortVmSupport.Less(items[right], items[mid])) ArraySortVmSupport.Swap(items, right, mid);

        // 把中位数换到 right 位置作为枢轴
        ArraySortVmSupport.Swap(items, mid, right);

        var pivot = items[right];
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (!ArraySortVmSupport.Less(items[j], pivot)) continue;
            i++;
            ArraySortVmSupport.Swap(items, i, j);
        }

        ArraySortVmSupport.Swap(items, i + 1, right);
        return i + 1;
    }
}
