using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Combinations(k) - 生成从列表中选择 k 个元素的所有组合
/// </summary>
public class ListCombinationsMethod : BaseInstanceMethod
{
    public override string[] Names => ["Combinations", "combinations"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["k"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var kValue = parameters[0].Run(manager);

        if (kValue is not IntLangValue kInt)
        {
            throw new ArgumentError(position, "k 参数必须是整数类型");
        }

        var k = kInt.Value;

        if (k < 0 || k > list.Values.Count)
        {
            throw new ArgumentError(position, $"k 必须在 0 到 {list.Values.Count} 之间");
        }

        var result = new List<LangValueType>();
        var current = new List<LangValueType>();
        GenerateCombinations(list.Values, k, 0, current, result);

        return new ListLangValue(result);
    }

    private static void GenerateCombinations(List<LangValueType> source, int k, int start,
        List<LangValueType> current, List<LangValueType> result)
    {
        if (current.Count == k)
        {
            result.Add(new ListLangValue(new List<LangValueType>(current)));
            return;
        }

        for (var i = start; i < source.Count; i++)
        {
            current.Add(source[i]);
            GenerateCombinations(source, k, i + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListCombinationsMethod).GetMethod(nameof(CombinationsHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> CombinationsHelper(List<object?> list, int k)
    {
        if (k < 0 || k > list.Count)
        {
            throw new ArgumentException($"k 必须在 0 到 {list.Count} 之间");
        }

        var result = new List<object?>();
        var current = new List<object?>();
        GenerateCombinationsHelper(list, k, 0, current, result);

        return result;
    }

    private static void GenerateCombinationsHelper(List<object?> source, int k, int start,
        List<object?> current, List<object?> result)
    {
        if (current.Count == k)
        {
            result.Add(new List<object?>(current));
            return;
        }

        for (var i = start; i < source.Count; i++)
        {
            current.Add(source[i]);
            GenerateCombinationsHelper(source, k, i + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (arguments[0] is not int k)
            {
                throw new ArgumentException("k 参数必须是整数类型");
            }

            return CombinationsHelper(list, k);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
