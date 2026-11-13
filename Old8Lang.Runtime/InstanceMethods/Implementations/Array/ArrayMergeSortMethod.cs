using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.MergeSort() - 使用归并排序算法对数组进行排序
/// </summary>
public class ArrayMergeSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["MergeSort", "mergeSort"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var items = array.GetItems().ToArray();
        MergeSortImpl(items, 0, items.Length - 1);
        return new ArrayLangValue(items, array.ElementType, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(ArrayMergeSortMethod).GetMethod(nameof(MergeSortHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ArrayLangValue MergeSortHelper(ArrayLangValue array)
    {
        var items = array.GetItems().ToArray();
        MergeSortImpl(items, 0, items.Length - 1);
        return new ArrayLangValue(items, array.ElementType, array.Position);
    }

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

    private static void Merge(LangValueType[] nums, int left, int mid, int right)
    {
        int n1 = mid - left + 1;
        int n2 = right - mid;

        var leftArray = new LangValueType[n1];
        var rightArray = new LangValueType[n2];

        System.Array.Copy(nums, left, leftArray, 0, n1);
        System.Array.Copy(nums, mid + 1, rightArray, 0, n2);

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

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ArrayLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var items = ArraySortVmSupport.Normalize(instance);
        if (items.Count > 1)
        {
            // 单一辅助缓冲区，避免每层归并都分配临时数组
            var buffer = new object?[items.Count];
            MergeSortRange(items, buffer, 0, items.Count - 1);
        }

        return items.ToArray();
    }

    /// <summary>VM 归并排序：自顶向下二分，合并时借助缓冲区，相等元素保持原顺序（稳定）。</summary>
    private static void MergeSortRange(List<object?> items, object?[] buffer, int left, int right)
    {
        if (left >= right) return;

        var mid = left + (right - left) / 2;
        MergeSortRange(items, buffer, left, mid);
        MergeSortRange(items, buffer, mid + 1, right);

        Merge(items, buffer, left, mid, right);
    }

    private static void Merge(List<object?> items, object?[] buffer, int left, int mid, int right)
    {
        for (var k = left; k <= right; k++)
            buffer[k] = items[k];

        int i = left, j = mid + 1, dest = left;

        while (i <= mid && j <= right)
        {
            // 只在右侧严格小于左侧时先取右侧，保证稳定
            if (ArraySortVmSupport.Less(buffer[j], buffer[i]))
                items[dest++] = buffer[j++];
            else
                items[dest++] = buffer[i++];
        }

        while (i <= mid) items[dest++] = buffer[i++];
        while (j <= right) items[dest++] = buffer[j++];
    }
}
