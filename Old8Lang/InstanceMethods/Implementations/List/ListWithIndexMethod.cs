using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.WithIndex() - 为每个元素添加索引，返回 (index, element) 元组列表
/// </summary>
public class ListWithIndexMethod : BaseInstanceMethod
{
    public override string[] Names => ["WithIndex", "withIndex"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var result = new List<LangValueType>();

        for (var i = 0; i < list.Values.Count; i++)
        {
            var tuple = new TupleLangValue(new IntLangValue(i), list.Values[i]);
            tuple.ItemValues.Add(new IntLangValue(i));
            tuple.ItemValues.Add(list.Values[i]);
            result.Add(tuple);
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListWithIndexMethod).GetMethod(nameof(WithIndexHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> WithIndexHelper(List<object?> list)
    {
        var result = new List<object?>();

        for (var i = 0; i < list.Count; i++)
        {
            result.Add(new object?[] { i, list[i] });
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
            return WithIndexHelper(list);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
