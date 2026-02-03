using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.HeapSort 方法 - 堆排序算法
/// </summary>
public class ListHeapSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["HeapSort", "heapSort"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var sorted = new List<LangValueType>(list.Values);

        HeapSort(sorted);

        return new ListLangValue(sorted);
    }

    /// <summary>
    /// 堆排序算法
    /// </summary>
    private static void HeapSort(List<LangValueType> list)
    {
        int n = list.Count;

        // 构建最大堆
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(list, n, i);
        }

        // 一个个从堆中取出元素
        for (int i = n - 1; i > 0; i--)
        {
            // 将当前最大元素移到末尾
            (list[0], list[i]) = (list[i], list[0]);

            // 对剩余元素重新堆化
            Heapify(list, i, 0);
        }
    }

    /// <summary>
    /// 堆化操作
    /// </summary>
    private static void Heapify(List<LangValueType> list, int n, int i)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        // 如果左子节点大于根节点
        if (left < n && Less(list[largest], list[left]))
        {
            largest = left;
        }

        // 如果右子节点大于当前最大值
        if (right < n && Less(list[largest], list[right]))
        {
            largest = right;
        }

        // 如果最大值不是根节点
        if (largest != i)
        {
            (list[i], list[largest]) = (list[largest], list[i]);

            // 递归堆化受影响的子树
            Heapify(list, n, largest);
        }
    }

    /// <summary>
    /// 比较两个值的大小
    /// </summary>
    private static bool Less(LangValueType a, LangValueType b)
    {
        if (a is IntLangValue intA && b is IntLangValue intB)
        {
            return intA.Value < intB.Value;
        }
        if (a is DoubleLangValue doubleA && b is DoubleLangValue doubleB)
        {
            return doubleA.Value < doubleB.Value;
        }
        if (a is StringLangValue strA && b is StringLangValue strB)
        {
            return string.Compare(strA.Value, strB.Value, StringComparison.Ordinal) < 0;
        }
        if (a is CharLangValue charA && b is CharLangValue charB)
        {
            return charA.Value < charB.Value;
        }

        throw new InvalidOperationException($"无法比较类型 {a.GetType().Name} 和 {b.GetType().Name}");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListHeapSortMethod).GetMethod(nameof(HeapSortHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：堆排序
    /// </summary>
    public static ListLangValue HeapSortHelper(ListLangValue list)
    {
        var sorted = new List<LangValueType>(list.Values);
        HeapSort(sorted);
        return new ListLangValue(sorted);
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
