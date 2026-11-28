using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.Flatten() - 扁平化嵌套列表（一层）
/// </summary>
public class ListFlattenMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
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
        instance.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        var helperMethod = typeof(ListFlattenMethod).GetMethod(nameof(FlattenNativeHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> FlattenNativeHelper(object? listObj)
    {
        if (listObj is List<object?> typed)
            return FlattenHelper(typed);
        if (listObj is System.Collections.IList ilist)
            return FlattenHelper(ilist.Cast<object?>().ToList());
        throw new ArgumentException($"实例必须是列表类型，实际是 {listObj?.GetType().Name ?? "null"}");
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
            else if (item is System.Collections.IList innerIList && item is not System.Array)
            {
                foreach (var sub in innerIList)
                {
                    result.Add(sub);
                }
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
