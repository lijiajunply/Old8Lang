using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Pairwise() - 将相邻元素配对，返回元组列表
/// </summary>
public class ListPairwiseMethod : BaseInstanceMethod
{
    public override string[] Names => ["Pairwise", "pairwise"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;

        if (list.Values.Count < 2)
        {
            return new ListLangValue(new List<LangValueType>());
        }

        var result = new List<LangValueType>();

        for (var i = 0; i < list.Values.Count - 1; i++)
        {
            var tuple = new TupleLangValue(list.Values[i], list.Values[i + 1]);
            tuple.ItemValues.Add(list.Values[i]);
            tuple.ItemValues.Add(list.Values[i + 1]);
            result.Add(tuple);
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListPairwiseMethod).GetMethod(nameof(PairwiseHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> PairwiseHelper(List<object?> list)
    {
        if (list.Count < 2)
        {
            return new List<object?>();
        }

        var result = new List<object?>();

        for (var i = 0; i < list.Count - 1; i++)
        {
            result.Add(new object?[] { list[i], list[i + 1] });
        }

        return result;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            return PairwiseHelper(list);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
