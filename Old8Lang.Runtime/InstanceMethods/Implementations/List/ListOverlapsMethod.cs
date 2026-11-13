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
/// List.Overlaps(other) - 检查两个列表是否有交集
/// </summary>
public class ListOverlapsMethod : BaseInstanceMethod
{
    public override string[] Names => ["Overlaps", "overlaps"];
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

        var otherSet = new HashSet<string>(otherList.Values.Select(v => v.ToDisplayString()));

        foreach (var item in list.Values)
        {
            if (otherSet.Contains(item.ToDisplayString()))
            {
                return new BoolLangValue(true);
            }
        }

        return new BoolLangValue(false);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListOverlapsMethod).GetMethod(nameof(OverlapsHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool OverlapsHelper(List<object?> list, List<object?> other)
    {
        var otherSet = new HashSet<string>(other.Select(v => v?.ToString() ?? "null"));

        foreach (var item in list)
        {
            if (otherSet.Contains(item?.ToString() ?? "null"))
            {
                return true;
            }
        }

        return false;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 支持 List<object?> 和 object?[] 两种类型
        List<object?> list;
        if (instance is List<object?> listInstance)
        {
            list = listInstance;
        }
        else if (instance is object?[] arrayInstance)
        {
            list = arrayInstance.ToList();
        }
        else
        {
            throw new ArgumentException("实例必须是 List<object?> 或 object?[] 类型");
        }

        if (arguments.Length > 0)
        {
            List<object?> other;
            if (arguments[0] is List<object?> otherList)
            {
                other = otherList;
            }
            else if (arguments[0] is object?[] otherArray)
            {
                other = otherArray.ToList();
            }
            else
            {
                throw new ArgumentException("other 参数必须是列表或数组类型");
            }

            return OverlapsHelper(list, other);
        }

        throw new ArgumentException("实例必须是 List<object?> 或 object?[] 类型");
    }
}
