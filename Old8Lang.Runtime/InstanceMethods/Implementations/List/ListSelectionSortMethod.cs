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
/// List.SelectionSort 方法 - 选择排序算法
/// </summary>
public class ListSelectionSortMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["SelectionSort", "selectionSort"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var sorted = new List<LangValueType>(list.Values);

        SelectionSort(sorted);

        return new ListLangValue(sorted);
    }

    /// <summary>
    /// 选择排序算法
    /// </summary>
    private static void SelectionSort(List<LangValueType> list)
    {
        int n = list.Count;

        for (int i = 0; i < n - 1; i++)
        {
            int minIndex = i;

            // 找到未排序部分的最小元素
            for (int j = i + 1; j < n; j++)
            {
                if (Less(list[j], list[minIndex]))
                {
                    minIndex = j;
                }
            }

            // 交换
            if (minIndex != i)
            {
                (list[i], list[minIndex]) = (list[minIndex], list[i]);
            }
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

        var helperMethod = typeof(ListSelectionSortMethod).GetMethod(nameof(SelectionSortNativeHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> SelectionSortNativeHelper(object? list) =>
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
