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
/// List.CartesianProduct(other) - 计算两个列表的笛卡尔积
/// </summary>
public class ListCartesianProductMethod : BaseInstanceMethod
{
    public override string[] Names => ["CartesianProduct", "cartesianProduct"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["other"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var otherValue = parameters[0].Run(manager);

        if (otherValue is not ListLangValue otherList)
        {
            throw new ArgumentError(position, "other 参数必须是列表类型");
        }

        var result = new List<LangValueType>();

        foreach (var item1 in list.Values)
        {
            foreach (var item2 in otherList.Values)
            {
                var tuple = new TupleLangValue(item1, item2);
                tuple.ItemValues.Add(item1);
                tuple.ItemValues.Add(item2);
                result.Add(tuple);
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListCartesianProductMethod).GetMethod(nameof(CartesianProductHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> CartesianProductHelper(List<object?> list, List<object?> other)
    {
        var result = new List<object?>();

        foreach (var item1 in list)
        {
            foreach (var item2 in other)
            {
                result.Add(new object?[] { item1, item2 });
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
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (arguments[0] is not List<object?> other)
            {
                throw new ArgumentException("other 参数必须是列表类型");
            }

            return CartesianProductHelper(list, other);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
