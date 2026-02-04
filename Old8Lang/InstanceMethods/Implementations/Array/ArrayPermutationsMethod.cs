using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.Permutations() - 生成数组所有元素的全排列
/// </summary>
public class ArrayPermutationsMethod : BaseInstanceMethod
{
    public override string[] Names => ["Permutations", "permutations"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var result = new List<LangValueType>();
        var items = array.RunResult.ToList();
        GeneratePermutations(items, 0, result);
        return new ListLangValue(result);
    }

    private static void GeneratePermutations(List<LangValueType> items, int start, List<LangValueType> result)
    {
        if (start >= items.Count)
        {
            result.Add(new ArrayLangValue(items.ToArray()));
            return;
        }

        for (var i = start; i < items.Count; i++)
        {
            // Swap
            (items[start], items[i]) = (items[i], items[start]);
            GeneratePermutations(items, start + 1, result);
            // Swap back
            (items[start], items[i]) = (items[i], items[start]);
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ArrayPermutationsMethod).GetMethod(nameof(PermutationsHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> PermutationsHelper(object?[] array)
    {
        var result = new List<object?>();
        var items = array.ToList();
        GeneratePermutationsHelper(items, 0, result);
        return result;
    }

    private static void GeneratePermutationsHelper(List<object?> items, int start, List<object?> result)
    {
        if (start >= items.Count)
        {
            result.Add(items.ToArray());
            return;
        }

        for (var i = start; i < items.Count; i++)
        {
            // Swap
            (items[start], items[i]) = (items[i], items[start]);
            GeneratePermutationsHelper(items, start + 1, result);
            // Swap back
            (items[start], items[i]) = (items[i], items[start]);
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is object?[] array)
        {
            return PermutationsHelper(array);
        }

        throw new ArgumentException("实例必须是 object?[] 类型");
    }
}
