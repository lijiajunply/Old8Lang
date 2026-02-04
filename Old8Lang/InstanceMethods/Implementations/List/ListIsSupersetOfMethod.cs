using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.IsSupersetOf(other) - 检查当前列表是否为另一个列表的超集
/// </summary>
public class ListIsSupersetOfMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsSupersetOf", "isSupersetOf"];
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
            throw new Error.ArgumentError(position, "other 参数必须是列表类型");
        }

        // 检查 otherList 中的每个元素是否都在 list 中
        foreach (var item in otherList.Values)
        {
            if (!list.Values.Any(x => x.Equal(item)))
            {
                return new BoolLangValue(false);
            }
        }

        return new BoolLangValue(true);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListIsSupersetOfMethod).GetMethod(nameof(IsSupersetOfHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool IsSupersetOfHelper(List<object?> list, List<object?> other)
    {
        // 检查 other 中的每个元素是否都在 list 中
        foreach (var item in other)
        {
            if (!list.Contains(item))
            {
                return false;
            }
        }

        return true;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (arguments[0] is not List<object?> other)
            {
                throw new ArgumentException("other 参数必须是列表类型");
            }

            return IsSupersetOfHelper(list, other);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
