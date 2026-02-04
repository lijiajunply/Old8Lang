using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.SymmetricExcept(other) - 返回两个列表的对称差集（在其中一个列表中但不在两个列表中的元素）
/// </summary>
public class ListSymmetricExceptMethod : BaseInstanceMethod
{
    public override string[] Names => ["SymmetricExcept", "symmetricExcept"];
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

        var result = new List<LangValueType>();
        var seen = new HashSet<string>();

        // 添加在 list 中但不在 otherList 中的元素
        foreach (var item in list.Values)
        {
            var key = item.ToDisplayString();
            if (!otherList.Values.Any(x => x.Equal(item)))
            {
                if (seen.Add(key))
                {
                    result.Add(item);
                }
            }
        }

        // 添加在 otherList 中但不在 list 中的元素
        foreach (var item in otherList.Values)
        {
            var key = item.ToDisplayString();
            if (!list.Values.Any(x => x.Equal(item)))
            {
                if (seen.Add(key))
                {
                    result.Add(item);
                }
            }
        }

        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListSymmetricExceptMethod).GetMethod(nameof(SymmetricExceptHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> SymmetricExceptHelper(List<object?> list, List<object?> other)
    {
        var result = new List<object?>();
        var seen = new HashSet<string>();

        // 添加在 list 中但不在 other 中的元素
        foreach (var item in list)
        {
            var key = item?.ToString() ?? "null";
            if (!other.Contains(item))
            {
                if (seen.Add(key))
                {
                    result.Add(item);
                }
            }
        }

        // 添加在 other 中但不在 list 中的元素
        foreach (var item in other)
        {
            var key = item?.ToString() ?? "null";
            if (!list.Contains(item))
            {
                if (seen.Add(key))
                {
                    result.Add(item);
                }
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

            return SymmetricExceptHelper(list, other);
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
