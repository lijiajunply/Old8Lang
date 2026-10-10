using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Implementations.Array;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Sort() - 排序（升序）
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListSortMethod : BaseLangListMethod
{
    public override string[] Names => ["Sort", "sort"];
    public override string[]? ParameterNames => ["comparer"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    /// <summary>
    /// 参数类型：无参数
    /// </summary>
    public override Type?[]? ParameterTypes => [typeof(FuncLangValue)];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(ListLangValue);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "对列表进行升序排序";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var sortedItems = new List<LangValueType>(items);

        if (parameters.Count == 1)
        {
            var comparerParam = parameters[0].Run(manager);
            if (comparerParam is not FuncLangValue comparer)
            {
                throw new ArgumentException("Sort 方法的 comparer 参数必须是函数");
            }

            QuickSortWithComparerInternal(sortedItems, 0, sortedItems.Count - 1, comparer, manager);
        }
        else
        {
            // 使用快速排序
            QuickSortInternal(sortedItems, 0, sortedItems.Count - 1);
        }

        return new ListLangValue(sortedItems, null, position);
    }

    private void QuickSortInternal(List<LangValueType> list, int left, int right)
    {
        if (left < right)
        {
            int pivotIndex = Partition(list, left, right);
            QuickSortInternal(list, left, pivotIndex - 1);
            QuickSortInternal(list, pivotIndex + 1, right);
        }
    }

    private int Partition(List<LangValueType> list, int left, int right)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (list[j].Less(pivot) || list[j].Equal(pivot))
            {
                i++;
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        (list[i + 1], list[right]) = (list[right], list[i + 1]);
        return i + 1;
    }

    private void QuickSortWithComparerInternal(List<LangValueType> list, int left, int right, FuncLangValue comparer,
        VariateManager manager)
    {
        if (left < right)
        {
            int pivotIndex = PartitionWithComparer(list, left, right, comparer, manager);
            QuickSortWithComparerInternal(list, left, pivotIndex - 1, comparer, manager);
            QuickSortWithComparerInternal(list, pivotIndex + 1, right, comparer, manager);
        }
    }

    private int PartitionWithComparer(List<LangValueType> list, int left, int right, FuncLangValue comparer,
        VariateManager manager)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            var result = comparer.Run(manager, [list[j], pivot]);
            if (result is IntLangValue intResult && intResult.Value < 0)
            {
                i++;
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        (list[i + 1], list[right]) = (list[right], list[i + 1]);
        return i + 1;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListSortMethod).GetMethod(nameof(SortHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue SortHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        var sortedItems = new List<LangValueType>(items);

        // 使用 LINQ OrderBy
        sortedItems = sortedItems.OrderBy(x => x, new LangValueComparer()).ToList();

        return new ListLangValue(sortedItems);
    }

    private class LangValueComparer : IComparer<LangValueType>
    {
        public int Compare(LangValueType? x, LangValueType? y)
        {
            if (x == null || y == null) return 0;
            if (x.Less(y)) return -1;
            if (x.Greater(y)) return 1;
            return 0;
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            return SortHelper(langList);
        }

        // VM 模式下数组字面量是原生 object?[]、列表是 List<object?>，都不实现 ILangList；
        // 用 GetItemsForVM 兜底（与 LangListCountMethod 的 ICollection 兜底同一模式），
        // 使 Array.Sort()/List.Sort() 在 VM 下可用。
        var items = GetItemsForVM(instance);

        if (arguments.Length > 0)
        {
            return SortNative(items, BuildVmComparison(arguments[0]));
        }

        return SortNative(items, ArraySortVmSupport.Compare);
    }

    /// <summary>
    /// 用稳定归并排序对原生元素排序并返回新列表（不修改接收者）。
    /// 自己实现而不用 List.Sort(comparer)，是为了在比较器不完全自洽时也不会抛
    /// "IComparer.Compare() 返回不一致结果"。
    /// </summary>
    private static List<object?> SortNative(List<object?> items, Comparison<object?> comparison)
    {
        var sorted = new List<object?>(items);
        if (sorted.Count > 1)
        {
            var buffer = new object?[sorted.Count];
            MergeSortRange(sorted, buffer, 0, sorted.Count - 1, comparison);
        }

        return sorted;
    }

    private static void MergeSortRange(List<object?> items, object?[] buffer, int left, int right,
        Comparison<object?> comparison)
    {
        if (left >= right) return;

        var mid = left + (right - left) / 2;
        MergeSortRange(items, buffer, left, mid, comparison);
        MergeSortRange(items, buffer, mid + 1, right, comparison);

        for (var k = left; k <= right; k++)
            buffer[k] = items[k];

        int i = left, j = mid + 1, dest = left;

        while (i <= mid && j <= right)
        {
            // 只在右侧严格小于左侧时先取右侧，保证稳定
            if (comparison(buffer[j], buffer[i]) < 0)
                items[dest++] = buffer[j++];
            else
                items[dest++] = buffer[i++];
        }

        while (i <= mid) items[dest++] = buffer[i++];
        while (j <= right) items[dest++] = buffer[j++];
    }

    /// <summary>
    /// 把 VM 中传入的比较器函数包装成 <see cref="Comparison{T}"/>；
    /// 调用失败或返回值无法转换时退回原生比较，避免静默地不排序。
    /// </summary>
    private static Comparison<object?> BuildVmComparison(object? comparer)
    {
        var vm = VMContext.CurrentVM;

        return (a, b) =>
        {
            try
            {
                var result = vm!.CallFunctionObject(comparer, [a, b]);
                if (result is int intResult) return intResult;
                return Convert.ToInt32(result);
            }
            catch
            {
                return ArraySortVmSupport.Compare(a, b);
            }
        };
    }
}
