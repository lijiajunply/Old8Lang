using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.GroupAdjacent() - 将相邻的相同元素分组
/// </summary>
public class ArrayGroupAdjacentMethod : BaseInstanceMethod
{
    public override string[] Names => ["GroupAdjacent", "groupAdjacent"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var result = new List<LangValueType>();

        if (array.RunResult.Length == 0)
        {
            return new ListLangValue(result);
        }

        var currentGroup = new List<LangValueType> { array.RunResult[0] };

        for (int i = 1; i < array.RunResult.Length; i++)
        {
            if (array.RunResult[i].Equal(array.RunResult[i - 1]))
            {
                currentGroup.Add(array.RunResult[i]);
            }
            else
            {
                result.Add(new ArrayLangValue(currentGroup.ToArray()));
                currentGroup = new List<LangValueType> { array.RunResult[i] };
            }
        }

        result.Add(new ArrayLangValue(currentGroup.ToArray()));
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ArrayGroupAdjacentMethod).GetMethod(nameof(GroupAdjacentHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> GroupAdjacentHelper(object?[] array)
    {
        var result = new List<object?>();

        if (array.Length == 0)
        {
            return result;
        }

        var currentGroup = new List<object?> { array[0] };

        for (int i = 1; i < array.Length; i++)
        {
            if (Equals(array[i], array[i - 1]))
            {
                currentGroup.Add(array[i]);
            }
            else
            {
                result.Add(currentGroup.ToArray());
                currentGroup = new List<object?> { array[i] };
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
        if (instance is object?[] array)
        {
            return GroupAdjacentHelper(array);
        }

        throw new ArgumentException("实例必须是 object?[] 类型");
    }
}
