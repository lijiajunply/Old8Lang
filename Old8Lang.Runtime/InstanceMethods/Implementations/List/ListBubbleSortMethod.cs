using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.BubbleSort 方法 - 冒泡排序算法
/// </summary>
public class ListBubbleSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["BubbleSort", "bubbleSort"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var sorted = new List<LangValueType>(list.Values);

        BubbleSort(sorted);

        return new ListLangValue(sorted);
    }

    /// <summary>
    /// 冒泡排序算法
    /// </summary>
    private static void BubbleSort(List<LangValueType> list)
    {
        int n = list.Count;

        for (int i = 0; i < n - 1; i++)
        {
            var swapped = false;

            for (int j = 0; j < n - i - 1; j++)
            {
                if (Less(list[j + 1], list[j]))
                {
                    (list[j], list[j + 1]) = (list[j + 1], list[j]);
                    swapped = true;
                }
            }

            // 如果没有发生交换，说明已经排序完成
            if (!swapped)
            {
                break;
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
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListBubbleSortMethod).GetMethod(nameof(BubbleSortHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：冒泡排序
    /// </summary>
    public static ListLangValue BubbleSortHelper(ListLangValue list)
    {
        var sorted = new List<LangValueType>(list.Values);
        BubbleSort(sorted);
        return new ListLangValue(sorted);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
        LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
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