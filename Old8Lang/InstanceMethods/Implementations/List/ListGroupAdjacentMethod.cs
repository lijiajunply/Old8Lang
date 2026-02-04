using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.GroupAdjacent() - 将相邻的相同元素分组
/// </summary>
public class ListGroupAdjacentMethod : BaseInstanceMethod
{
    public override string[] Names => ["GroupAdjacent", "groupAdjacent"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var result = new List<LangValueType>();

        if (list.Values.Count == 0)
        {
            return new ListLangValue(result);
        }

        var currentGroup = new List<LangValueType> { list.Values[0] };

        for (int i = 1; i < list.Values.Count; i++)
        {
            if (list.Values[i].Equal(list.Values[i - 1]))
            {
                currentGroup.Add(list.Values[i]);
            }
            else
            {
                result.Add(new ListLangValue(currentGroup));
                currentGroup = new List<LangValueType> { list.Values[i] };
            }
        }

        result.Add(new ListLangValue(currentGroup));
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListGroupAdjacentMethod).GetMethod(nameof(GroupAdjacentHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> GroupAdjacentHelper(List<object?> list)
    {
        var result = new List<object?>();

        if (list.Count == 0)
        {
            return result;
        }

        var currentGroup = new List<object?> { list[0] };

        for (int i = 1; i < list.Count; i++)
        {
            if (Equals(list[i], list[i - 1]))
            {
                currentGroup.Add(list[i]);
            }
            else
            {
                result.Add(currentGroup.ToArray());
                currentGroup = new List<object?> { list[i] };
            }
        }

        result.Add(currentGroup.ToArray());
        return result;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 支持 List<object?> 和 object?[] 两种类型
        if (instance is List<object?> list)
        {
            return GroupAdjacentHelper(list);
        }
        else if (instance is object?[] array)
        {
            return GroupAdjacentHelper(array.ToList());
        }

        throw new ArgumentException("实例必须是 List<object?> 或 object?[] 类型");
    }
}
