using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.MergeSort 方法 - 归并排序算法
/// </summary>
public class ListMergeSortMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["MergeSort", "mergeSort"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var sorted = new List<LangValueType>(list.Values);

        MergeSort(sorted, 0, sorted.Count - 1);

        return new ListLangValue(sorted);
    }

    /// <summary>
    /// 归并排序算法
    /// </summary>
    private static void MergeSort(List<LangValueType> list, int left, int right)
    {
        if (left < right)
        {
            int mid = left + (right - left) / 2;
            MergeSort(list, left, mid);
            MergeSort(list, mid + 1, right);
            Merge(list, left, mid, right);
        }
    }

    /// <summary>
    /// 合并两个已排序的子数组
    /// </summary>
    private static void Merge(List<LangValueType> list, int left, int mid, int right)
    {
        int n1 = mid - left + 1;
        int n2 = right - mid;

        var leftArray = new LangValueType[n1];
        var rightArray = new LangValueType[n2];

        for (int i = 0; i < n1; i++)
        {
            leftArray[i] = list[left + i];
        }
        for (int j = 0; j < n2; j++)
        {
            rightArray[j] = list[mid + 1 + j];
        }

        int iIndex = 0, jIndex = 0;
        int k = left;

        while (iIndex < n1 && jIndex < n2)
        {
            if (Less(leftArray[iIndex], rightArray[jIndex]))
            {
                list[k] = leftArray[iIndex];
                iIndex++;
            }
            else
            {
                list[k] = rightArray[jIndex];
                jIndex++;
            }
            k++;
        }

        while (iIndex < n1)
        {
            list[k] = leftArray[iIndex];
            iIndex++;
            k++;
        }

        while (jIndex < n2)
        {
            list[k] = rightArray[jIndex];
            jIndex++;
            k++;
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
        instance.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        var helperMethod = typeof(ListMergeSortMethod).GetMethod(nameof(MergeSortNativeHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> MergeSortNativeHelper(object? list) =>
        Array.ArraySortVmSupport.SortToListDefault(list);

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return Array.ArraySortVmSupport.SortToListDefault(instance);
    }
}
