using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.InsertionSort() - 使用插入排序算法对数组进行排序
/// </summary>
public class ArrayInsertionSortMethod : BaseInstanceMethod
{
    public override string[] Names => ["InsertionSort", "insertionSort"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var items = array.GetItems().ToArray();
        InsertionSortImpl(items);
        return new ArrayLangValue(items, array.ElementType, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(ArrayInsertionSortMethod).GetMethod(nameof(InsertionSortHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ArrayLangValue InsertionSortHelper(ArrayLangValue array)
    {
        var items = array.GetItems().ToArray();
        InsertionSortImpl(items);
        return new ArrayLangValue(items, array.ElementType, array.Position);
    }

    private static void InsertionSortImpl(LangValueType[] nums)
    {
        int n = nums.Length;
        for (int i = 1; i < n; i++)
        {
            var key = nums[i];
            int j = i - 1;

            while (j >= 0 && key.Less(nums[j]))
            {
                nums[j + 1] = nums[j];
                j--;
            }
            nums[j + 1] = key;
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ArrayLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("Array.InsertionSort 方法在 VM 模式下暂不支持");
    }
}
