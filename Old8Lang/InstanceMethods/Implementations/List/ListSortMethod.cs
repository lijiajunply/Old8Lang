using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Sort 方法 - 对列表进行排序（默认使用快速排序）
/// </summary>
public class ListSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["Sort", "sort"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        // 创建新列表副本以避免修改原列表
        var sortedValues = new List<LangValueType>(list.Values);
        QuickSort(sortedValues, 0, sortedValues.Count - 1);
        return new ListLangValue(sortedValues);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var sortHelperMethod = typeof(ListSortMethod).GetMethod(nameof(SortHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, sortHelperMethod!);
    }

    /// <summary>
    /// 辅助方法：排序列表
    /// </summary>
    public static ListLangValue SortHelper(ListLangValue list)
    {
        var sortedValues = new List<LangValueType>(list.Values);
        QuickSort(sortedValues, 0, sortedValues.Count - 1);
        return new ListLangValue(sortedValues);
    }

    /// <summary>
    /// 快速排序实现
    /// </summary>
    private static void QuickSort(List<LangValueType> nums, int left, int right)
    {
        if (left < right)
        {
            int pivotIndex = Partition(nums, left, right);
            QuickSort(nums, left, pivotIndex - 1);
            QuickSort(nums, pivotIndex + 1, right);
        }
    }

    private static int Partition(List<LangValueType> nums, int left, int right)
    {
        var pivot = nums[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (nums[j].Less(pivot))
            {
                i++;
                (nums[i], nums[j]) = (nums[j], nums[i]);
            }
        }

        (nums[i + 1], nums[right]) = (nums[right], nums[i + 1]);
        return i + 1;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            var sorted = new List<object?>(list);
            sorted.Sort();
            return sorted;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
