using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.BubbleSort() - 使用冒泡排序算法对数组进行排序
/// </summary>
public class ArrayBubbleSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["BubbleSort", "bubbleSort"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var items = array.GetItems().ToArray();
        BubbleSortImpl(items);
        return new ArrayLangValue(items, array.ElementType, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(ArrayBubbleSortMethod).GetMethod(nameof(BubbleSortHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ArrayLangValue BubbleSortHelper(ArrayLangValue array)
    {
        var items = array.GetItems().ToArray();
        BubbleSortImpl(items);
        return new ArrayLangValue(items, array.ElementType, array.Position);
    }

    private static void BubbleSortImpl(LangValueType[] nums)
    {
        int n = nums.Length;
        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - i - 1; j++)
            {
                if (nums[j + 1].Less(nums[j]))
                {
                    (nums[j], nums[j + 1]) = (nums[j + 1], nums[j]);
                    swapped = true;
                }
            }
            if (!swapped) break;
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ArrayLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("Array.BubbleSort 方法在 VM 模式下暂不支持");
    }
}
