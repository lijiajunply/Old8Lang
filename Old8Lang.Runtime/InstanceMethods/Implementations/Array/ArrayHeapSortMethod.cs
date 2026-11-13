using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.HeapSort() - 使用堆排序算法对数组进行排序
/// </summary>
public class ArrayHeapSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["HeapSort", "heapSort"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var items = array.GetItems().ToArray();
        HeapSortImpl(items);
        return new ArrayLangValue(items, array.ElementType, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(ArrayHeapSortMethod).GetMethod(nameof(HeapSortHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ArrayLangValue HeapSortHelper(ArrayLangValue array)
    {
        var items = array.GetItems().ToArray();
        HeapSortImpl(items);
        return new ArrayLangValue(items, array.ElementType, array.Position);
    }

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
            (nums[0], nums[i]) = (nums[i], nums[0]);
            Heapify(nums, i, 0);
        }
    }

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
            (nums[i], nums[largest]) = (nums[largest], nums[i]);
            Heapify(nums, n, largest);
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ArrayLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return ArraySortVmSupport.SortAndReturn(instance, HeapSort);
    }

    /// <summary>VM 堆排序：先建最大堆，再逐个把堆顶换到末尾并下沉修复。</summary>
    private static void HeapSort(List<object?> items)
    {
        var n = items.Count;

        // 构建最大堆
        for (var i = n / 2 - 1; i >= 0; i--)
            SiftDown(items, n, i);

        // 逐个提取堆顶（当前最大值）到数组末尾
        for (var i = n - 1; i > 0; i--)
        {
            ArraySortVmSupport.Swap(items, 0, i);
            SiftDown(items, i, 0);
        }
    }

    private static void SiftDown(List<object?> items, int n, int i)
    {
        while (true)
        {
            var largest = i;
            var left = 2 * i + 1;
            var right = 2 * i + 2;

            if (left < n && ArraySortVmSupport.Less(items[largest], items[left]))
                largest = left;

            if (right < n && ArraySortVmSupport.Less(items[largest], items[right]))
                largest = right;

            if (largest == i) return;

            ArraySortVmSupport.Swap(items, i, largest);
            i = largest;
        }
    }
}
