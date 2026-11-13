using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.FlattenDeep() - 深度扁平化嵌套列表（递归扁平化所有层级）
/// </summary>
public class ListFlattenDeepMethod : BaseInstanceMethod
{
    public override string[] Names => ["FlattenDeep", "flattenDeep"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var result = new List<LangValueType>();
        FlattenRecursive(list.Values, result);
        return new ListLangValue(result);
    }

    private static void FlattenRecursive(List<LangValueType> source, List<LangValueType> result)
    {
        foreach (var item in source)
        {
            if (item is ListLangValue innerList)
            {
                FlattenRecursive(innerList.Values, result);
            }
            else
            {
                result.Add(item);
            }
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListFlattenDeepMethod).GetMethod(nameof(FlattenDeepHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> FlattenDeepHelper(List<object?> list)
    {
        var result = new List<object?>();
        FlattenRecursiveHelper(list, result);
        return result;
    }

    private static void FlattenRecursiveHelper(List<object?> source, List<object?> result)
    {
        foreach (var item in source)
        {
            if (item is List<object?> innerList)
            {
                FlattenRecursiveHelper(innerList, result);
            }
            else
            {
                result.Add(item);
            }
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list)
        {
            return FlattenDeepHelper(list);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
