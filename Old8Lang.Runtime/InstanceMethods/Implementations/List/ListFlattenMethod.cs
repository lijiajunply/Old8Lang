using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Flatten() - 扁平化嵌套列表（一层）
/// </summary>
public class ListFlattenMethod : BaseInstanceMethod
{
    public override string[] Names => ["Flatten", "flatten"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var result = new List<LangValueType>();

        foreach (var item in list.Values)
        {
            if (item is ListLangValue innerList)
            {
                result.AddRange(innerList.Values);
            }
            else
            {
                result.Add(item);
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListFlattenMethod).GetMethod(nameof(FlattenHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> FlattenHelper(List<object?> list)
    {
        var result = new List<object?>();

        foreach (var item in list)
        {
            if (item is List<object?> innerList)
            {
                result.AddRange(innerList);
            }
            else
            {
                result.Add(item);
            }
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
            return FlattenHelper(list);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
